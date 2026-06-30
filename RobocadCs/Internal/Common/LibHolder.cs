using System;
using System.Runtime.InteropServices;

namespace RobocadCs.Internal.Common
{
    public class LibHolder : IDisposable
    {
        [DllImport("libdl.so.2", CharSet = CharSet.Ansi)]
        private static extern IntPtr dlopen(string filename, int flags);
        [DllImport("libdl.so.2", CharSet = CharSet.Ansi)]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);
        [DllImport("libdl.so.2")]
        private static extern int dlclose(IntPtr handle);
        private const int RTLD_LAZY = 0x0001;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate int StartSpiDelegate(string path, int channel, int speed, int mode);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate int StartUsbDelegate(string path, int baud);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr ReadWriteDelegate(byte[] data, uint len);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void StopDelegate();

        private IntPtr _handle;
        private readonly StartSpiDelegate _startSpi;
        private readonly StartUsbDelegate _startUsb;
        private readonly ReadWriteDelegate _readWriteSpi;
        private readonly ReadWriteDelegate _readWriteUsb;
        private readonly StopDelegate _stopSpi;
        private readonly StopDelegate _stopUsb;

        public LibHolder(string firstPath)
        {
            string libPath = firstPath + "/CommonRPiLibrary/CommonRPiLibrary/build/libCommonRPiLibrary.so";
            _handle = dlopen(libPath, RTLD_LAZY);
            if (_handle == IntPtr.Zero)
                throw new Exception("Failed to load library: " + libPath);

            _startSpi = GetDelegate<StartSpiDelegate>("StartSPI");
            _startUsb = GetDelegate<StartUsbDelegate>("StartUSB");
            _readWriteSpi = GetDelegate<ReadWriteDelegate>("ReadWriteSPI");
            _readWriteUsb = GetDelegate<ReadWriteDelegate>("ReadWriteUSB");
            _stopSpi = GetDelegate<StopDelegate>("StopSPI");
            _stopUsb = GetDelegate<StopDelegate>("StopUSB");
        }

        private T GetDelegate<T>(string symbol) where T : class
        {
            IntPtr p = dlsym(_handle, symbol);
            if (p == IntPtr.Zero) throw new Exception("Failed to load symbol: " + symbol);
            return Marshal.GetDelegateForFunctionPointer(p, typeof(T)) as T;
        }

        public int InitSpi(string path, int channel, int speed, int mode) => _startSpi(path, channel, speed, mode);
        public int InitUsb(string path, int baud) => _startUsb(path, baud);

        public byte[] RwSpi(byte[] data)
        {
            IntPtr result = _readWriteSpi(data, (uint)data.Length);
            return Copy(result, data.Length);
        }

        public byte[] RwUsb(byte[] data)
        {
            IntPtr result = _readWriteUsb(data, (uint)data.Length);
            return Copy(result, data.Length);
        }

        public void StopSpi() => _stopSpi?.Invoke();
        public void StopUsb() => _stopUsb?.Invoke();

        private static byte[] Copy(IntPtr ptr, int len)
        {
            if (ptr == IntPtr.Zero) return Array.Empty<byte>();
            byte[] outBuf = new byte[len];
            Marshal.Copy(ptr, outBuf, 0, len);
            return outBuf;
        }

        public void Dispose()
        {
            if (_handle != IntPtr.Zero)
            {
                dlclose(_handle);
                _handle = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }
    }
}
