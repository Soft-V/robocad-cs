using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace RobocadCs.Internal.Common
{
    // One controlling application at a time, sharing pigpiod's text FIFOs.
    public sealed class PigpioPwm
    {
        private const int PwmRange = 10000;
        private readonly string _pipePath, _responsePath;
        private readonly int _frequency;
        private readonly double _timeout;
        private readonly object _gate = new object();
        private readonly HashSet<int> _configured = new HashSet<int>();
        private readonly HashSet<int> _used = new HashSet<int>();
        private (uint Major, uint Minor, ulong Inode)? _identity;
        private bool _stopped;

        public PigpioPwm(string pipePath = "/dev/pigpio", string responsePath = "/dev/pigout",
                         int frequency = 100, double timeout = 1.0)
        {
            if (frequency <= 0) throw new ArgumentOutOfRangeException(nameof(frequency));
            if (!(timeout > 0 && timeout <= int.MaxValue / 1000.0))
                throw new ArgumentOutOfRangeException(nameof(timeout));
            _pipePath = pipePath;
            _responsePath = responsePath;
            _frequency = frequency;
            _timeout = timeout;
        }

        // statx has the same fixed ABI on Linux ARM32, ARM64 and x86_64.
        // Unlike struct stat, its offsets do not depend on the architecture.
        [StructLayout(LayoutKind.Explicit, Size = 256)]
        private struct Statx
        {
            [FieldOffset(0)] public uint Mask;
            [FieldOffset(28)] public ushort Mode;
            [FieldOffset(32)] public ulong Inode;
            [FieldOffset(136)] public uint DevMajor;
            [FieldOffset(140)] public uint DevMinor;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PollFd
        {
            public int Fd;
            public short Events, Revents;
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int open(string path, int flags);
        [DllImport("libc", SetLastError = true)]
        private static extern int close(int fd);
        [DllImport("libc", SetLastError = true)]
        private static extern IntPtr read(int fd, [Out] byte[] buffer, UIntPtr size);
        [DllImport("libc", SetLastError = true)]
        private static extern IntPtr write(int fd, byte[] buffer, UIntPtr size);
        [DllImport("libc", SetLastError = true)]
        private static extern int poll(ref PollFd fd, UIntPtr count, int timeout);
        [DllImport("libc", SetLastError = true)]
        private static extern int statx(int dirfd, string path, int flags, uint mask, out Statx info);

        private static IOException NativeError(string operation) =>
            new IOException(operation + ": errno " + Marshal.GetLastWin32Error() +
                            ". Start pigpiod with FIFO enabled and check permissions.");

        private static int OpenFifo(string path, int mode, out Statx info)
        {
            // No O_CREAT or O_TRUNC: never create/overwrite ordinary files.
            int fd = open(path, mode | 0x800 | 0x80000); // O_NONBLOCK | O_CLOEXEC
            if (fd < 0) throw NativeError("Cannot open " + path);
            try
            {
                if (statx(fd, "", 0x1000, 0x101, out info) != 0) // AT_EMPTY_PATH, TYPE | INO
                    throw NativeError("Cannot inspect " + path);
                if ((info.Mask & 0x101) != 0x101 || (info.Mode & 0xF000) != 0x1000)
                    throw new IOException(path + " is not a pigpio FIFO");
                return fd;
            }
            catch { close(fd); throw; }
        }

        private int Command(int commandFd, int responseFd, string command)
        {
            byte[] data = Encoding.ASCII.GetBytes(command + "\n");
            long written;
            do { written = write(commandFd, data, (UIntPtr)data.Length).ToInt64(); }
            while (written < 0 && Marshal.GetLastWin32Error() == 4); // EINTR
            if (written != data.Length) throw NativeError("Cannot write pigpio command " + command);
            var timer = Stopwatch.StartNew();
            var response = new StringBuilder();
            byte[] buffer = new byte[64];
            while (response.ToString().IndexOf('\n') < 0)
            {
                double remaining = _timeout - timer.Elapsed.TotalSeconds;
                if (remaining <= 0) throw new IOException("Timed out waiting for pigpiod: " + command);
                var fd = new PollFd { Fd = responseFd, Events = 1 }; // POLLIN
                int ready = poll(ref fd, (UIntPtr)1, (int)Math.Ceiling(remaining * 1000));
                if (ready < 0 && Marshal.GetLastWin32Error() == 4) continue;
                if (ready < 0) throw NativeError("Cannot poll pigpio response FIFO");
                if (ready == 0) continue;
                long count = read(responseFd, buffer, (UIntPtr)buffer.Length).ToInt64();
                int error = Marshal.GetLastWin32Error();
                if (count < 0 && (error == 4 || error == 11)) continue;
                if (count <= 0) throw new IOException("pigpiod closed its response FIFO");
                response.Append(Encoding.ASCII.GetString(buffer, 0, (int)count));
                if (response.Length > 64) throw new IOException("Oversized pigpio response");
            }
            if (!int.TryParse(response.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                throw new IOException("Invalid pigpio response: " + response);
            if (result < 0) throw new IOException($"pigpio command '{command}' failed with error {result}");
            return result;
        }

        public void SetDutycycle(int gpio, double duty)
        {
            if (gpio < 0 || gpio > 31) throw new ArgumentOutOfRangeException(nameof(gpio));
            if (!(duty >= 0 && duty <= 1)) throw new ArgumentOutOfRangeException(nameof(duty));
            lock (_gate)
            {
                if (_stopped && duty != 0) throw new InvalidOperationException("PWM controller has been stopped");
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    throw new PlatformNotSupportedException("pigpio FIFO control requires Linux");
                int input = -1, output = -1;
                try
                {
                    input = OpenFifo(_pipePath, 1, out Statx info); // O_WRONLY
                    output = OpenFifo(_responsePath, 0, out _); // O_RDONLY
                    var identity = (info.DevMajor, info.DevMinor, info.Inode);
                    if (_identity != identity)
                    {
                        _configured.Clear();
                        _identity = identity;
                    }
                    byte[] stale = new byte[4096];
                    var timer = Stopwatch.StartNew();
                    while (true)
                    {
                        long count = read(output, stale, (UIntPtr)stale.Length).ToInt64();
                        int error = Marshal.GetLastWin32Error();
                        if (count == 0 || (count < 0 && error == 11)) break;
                        if (count < 0 && error != 4) throw NativeError("Cannot drain pigpio response FIFO");
                        if (timer.Elapsed.TotalSeconds >= _timeout)
                            throw new IOException("pigpio response FIFO is busy; use one controlling application");
                    }
                    string pin = gpio.ToString(CultureInfo.InvariantCulture);
                    if (duty == 0)
                    {
                        Command(input, output, "W " + pin + " 0");
                        _configured.Remove(gpio);
                        _used.Remove(gpio);
                        return;
                    }
                    // A restarted daemon can reuse the same FIFO inode.
                    if (_configured.Contains(gpio) &&
                        (Command(input, output, "PFG " + pin) != _frequency ||
                         Command(input, output, "PRG " + pin) != PwmRange))
                        _configured.Remove(gpio);
                    if (!_configured.Contains(gpio))
                    {
                        Command(input, output, "P " + pin + " 0");
                        int actual = Command(input, output, "PFS " + pin + " " + _frequency.ToString(CultureInfo.InvariantCulture));
                        if (actual != _frequency)
                            throw new IOException($"pigpio selected {actual} Hz instead of {_frequency}; check sample rate (100 Hz works with -s 5)");
                        Command(input, output, "PRS " + pin + " " + PwmRange.ToString(CultureInfo.InvariantCulture));
                        _configured.Add(gpio);
                    }
                    _used.Add(gpio); // A lost reply does not mean PWM was not applied.
                    int value = (int)Math.Round(duty * PwmRange, MidpointRounding.ToEven);
                    Command(input, output, "P " + pin + " " + value.ToString(CultureInfo.InvariantCulture));
                }
                catch { _configured.Clear(); throw; }
                finally
                {
                    if (output >= 0) close(output);
                    if (input >= 0) close(input);
                }
            }
        }

        // Removes pulses; releasing torque depends on the servo electronics.
        public void Stop(IEnumerable<int> gpios = null)
        {
            lock (_gate)
            {
                _stopped = true;
                var targets = new SortedSet<int>(_used);
                if (gpios != null) targets.UnionWith(gpios);
                var errors = new List<Exception>();
                foreach (int gpio in targets)
                {
                    try { SetDutycycle(gpio, 0); }
                    catch (Exception error) { errors.Add(error); }
                }
                if (errors.Count != 0) throw new AggregateException("Failed to disable HCDIO PWM", errors);
            }
        }
    }
}
