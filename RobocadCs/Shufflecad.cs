using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using OpenCvSharp;
using RobocadCs.Internal.Common;

namespace RobocadCs
{
    public class Shufflecad
    {
        public const string LogInfo = "info";
        public const string LogWarning = "warning";
        public const string LogError = "error";

        public static Func<Mat, byte[]> JpegEncoder = OpenCvImaging.EncodeJpeg;

        internal readonly object DataLock = new object();
        internal readonly List<ShuffleVariable> Variables = new List<ShuffleVariable>();
        internal readonly List<CameraVariable> CameraVariables = new List<CameraVariable>();
        internal readonly JoystickData _joystickData = new JoystickData();
        internal readonly List<string> PrintArray = new List<string>();

        private readonly Robot _robot;
        private readonly ConnectionHelper _conn;

        public Shufflecad(Robot robot)
        {
            _robot = robot;
            SignalHandler.Register(Stop);
            _conn = new ConnectionHelper(this, robot);
        }

        public void Stop() => _conn.Stop();

        public ShuffleVariable AddVar(ShuffleVariable var)
        {
            lock (DataLock) { Variables.Add(var); }
            return var;
        }

        public CameraVariable AddVar(CameraVariable var)
        {
            lock (DataLock) { CameraVariables.Add(var); }
            return var;
        }

        public JoystickData JoystickData
        {
            get { lock (DataLock) { return _joystickData; } }
        }

        public void PrintToLog(string message, string messageType = LogInfo, string color = "#808080" )
        {
            lock (DataLock) { PrintArray.Add(messageType + "@" + message + color); }
        }
    }

    public class JoystickData
    {
        public bool BtnA { get; set; }  
        public bool BtnB { get; set; }  
        public bool BtnX { get; set; }  
        public bool BtnY { get; set; }
        
        public bool DpudUp { get; set; }  
        public bool DpudDown { get; set; }  
        public bool DpudLeft { get; set; }  
        public bool DpudRight { get; set; }  
        
        public byte RightTrigger { get; set; }  
        public byte LeftTrigger { get; set; }  
        
        public int RightStickX { get; set; }  
        public int RightStickY { get; set; }
        
        public int LeftStickY { get; set; }  
        public int LeftStickX { get; set; }  
        
        public bool RightShoulder { get; set; }
        public bool LeftShoulder { get; set; }
    }

    public class ShuffleVariable
    {
        public const string FloatType = "float";
        public const string StringType = "string";
        public const string BigStringType = "bigstring";
        public const string BoolType = "bool";
        public const string ChartType = "chart";
        public const string SliderType = "slider";
        public const string RadarType = "radar";
        public const string InVar = "in";
        public const string OutVar = "out";

        public string Name;
        public string Type;
        public string Direction;
        private string _value = "";
        private readonly object _lock = new object();

        public ShuffleVariable(string name, string type, string direction = InVar)
        {
            Name = name; Type = type; Direction = direction;
        }

        internal string Value
        {
            get { lock (_lock) return _value; }
            set { lock (_lock) _value = value; }
        }

        public void SetBool(bool value) => Value = value ? "1" : "0";
        public void SetFloat(float value) => Value = value.ToString(CultureInfo.InvariantCulture);
        public void SetString(string value) => Value = value;

        public void SetRadar(IList<float> values)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < values.Count; i++)
            {
                sb.Append(i).Append('+').Append(values[i].ToString(CultureInfo.InvariantCulture));
                if (i != values.Count - 1) sb.Append('+');
            }
            Value = sb.ToString();
        }

        public bool GetBool() => Value == "1";

        public float GetFloat()
        {
            string v = Value;
            if (string.IsNullOrEmpty(v)) return 0f;
            v = v.Replace(',', '.');
            return float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float r) ? r : 0f;
        }

        public string GetString() => Value;
    }

    public class CameraVariable
    {
        public string Name;
        private Mat _frame;
        public int Width;
        public int Height;
        private readonly object _lock = new object();

        public CameraVariable(string name) { Name = name; }

        // Stores a private copy of the frame; the caller keeps ownership of theirs.
        public void SetMat(Mat frame)
        {
            if (frame == null || frame.Empty()) return;
            Mat copy = frame.Clone();
            lock (_lock)
            {
                _frame?.Dispose();
                _frame = copy;
                Width = copy.Width;
                Height = copy.Height;
            }
        }

        internal byte[] GetValue()
        {
            // Encode under the lock: SetMat may Dispose the stored Mat concurrently.
            lock (_lock)
            {
                if (_frame != null && Shufflecad.JpegEncoder != null)
                {
                    try { return Shufflecad.JpegEncoder(_frame) ?? Encoding.UTF8.GetBytes("null"); }
                    catch (Exception e) { Console.Error.WriteLine("JPEG encode error: " + e.Message); return Encoding.UTF8.GetBytes("null"); }
                }
            }
            return Encoding.UTF8.GetBytes("null");
        }
    }

    internal sealed class SplitFrames
    {
        private readonly Socket _sock;
        public SplitFrames(Socket s) { _sock = s; }

        public bool WriteData(byte[] data)
        {
            byte[] len = SocketUtil.Uint32Le((uint)data.Length);
            return SocketUtil.SendAll(_sock, len) && (data.Length == 0 || SocketUtil.SendAll(_sock, data));
        }

        public bool WriteString(string s) => WriteData(Encoding.UTF8.GetBytes(s));

        public byte[] ReadData()
        {
            byte[] lenBuf = new byte[4];
            if (!SocketUtil.ReceiveAll(_sock, lenBuf, 4)) return Array.Empty<byte>();
            int size = (int)SocketUtil.ReadUint32Le(lenBuf);
            byte[] buf = new byte[size];
            if (size > 0 && !SocketUtil.ReceiveAll(_sock, buf, size)) return Array.Empty<byte>();
            return buf;
        }

        public string ReadString()
        {
            byte[] d = ReadData();
            return d.Length == 0 ? "null" : Encoding.UTF8.GetString(d);
        }
    }

    internal abstract class BasePort
    {
        protected readonly int Port;
        protected readonly float Delay;
        protected readonly Action EventHandler;
        protected volatile bool StopThread;
        protected Thread Worker;
        protected Socket ServerFd;

        public string OutString = "null";
        public byte[] OutBytes = Encoding.UTF8.GetBytes("null");
        public string StrFromClient = "-1";

        protected BasePort(int port, Action handler, float delay)
        {
            Port = port; EventHandler = handler; Delay = delay;
        }

        public void Stop()
        {
            StopThread = true;
            try { ServerFd?.Close(); } catch { }
            try { Worker?.Join(1000); } catch { }
        }

        protected Socket WaitForConnection()
        {
            try
            {
                ServerFd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                ServerFd.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                ServerFd.Bind(new IPEndPoint(IPAddress.Any, Port));
                ServerFd.Listen(1);
                return ServerFd.Accept();
            }
            catch { return null; }
        }

        protected int DelayMs => (int)(Delay * 1000);
    }

    internal sealed class ShflTalkPort : BasePort
    {
        private readonly bool _isCamera;
        public ShflTalkPort(int port, Action handler, float delay, bool isCamera = false)
            : base(port, handler, delay) { _isCamera = isCamera; }

        public void StartTalking()
        {
            Worker = new Thread(() =>
            {
                Socket client = WaitForConnection();
                if (client == null) return;
                var h = new SplitFrames(client);
                while (!StopThread)
                {
                    EventHandler?.Invoke();
                    if (_isCamera)
                    {
                        if (!h.WriteString(OutString)) break;
                        h.ReadData();
                        if (!h.WriteData(OutBytes)) break;
                        StrFromClient = h.ReadString();
                    }
                    else
                    {
                        if (!h.WriteString(OutString)) break;
                        StrFromClient = h.ReadString();
                    }
                    Thread.Sleep(DelayMs);
                }
                try { client.Close(); } catch { }
            }) { IsBackground = true };
            Worker.Start();
        }
    }

    internal sealed class ShflListenPort : BasePort
    {
        public ShflListenPort(int port, Action handler, float delay) : base(port, handler, delay) { }

        public void StartListening()
        {
            Worker = new Thread(() =>
            {
                Socket client = WaitForConnection();
                if (client == null) return;
                var h = new SplitFrames(client);
                while (!StopThread)
                {
                    if (!h.WriteString("Waiting for data")) break;
                    OutString = h.ReadString();
                    EventHandler?.Invoke();
                    Thread.Sleep(DelayMs);
                }
                try { client.Close(); } catch { }
            }) { IsBackground = true };
            Worker.Start();
        }
    }

    internal sealed class ConnectionHelper
    {
        private readonly Shufflecad _sc;
        private readonly Robot _robot;
        private int _cameraToggler;

        private readonly ShflTalkPort _out;
        private readonly ShflListenPort _in;
        private readonly ShflTalkPort _chart;
        private readonly ShflTalkPort _outcad;
        private readonly ShflTalkPort _rpi;
        private readonly ShflTalkPort _camera;
        private readonly ShflListenPort _joy;

        public ConnectionHelper(Shufflecad sc, Robot robot)
        {
            _sc = sc; _robot = robot;
            _out = new ShflTalkPort(63253, OnOutVars, 0.004f);
            _in = new ShflListenPort(63258, OnInVars, 0.004f);
            _chart = new ShflTalkPort(63255, OnChartVars, 0.002f);
            _outcad = new ShflTalkPort(63257, OnOutcadVars, 0.1f);
            _rpi = new ShflTalkPort(63256, OnRpiVars, 0.5f);
            _camera = new ShflTalkPort(63254, OnCameraVars, 0.03f, true);
            _joy = new ShflListenPort(63259, OnJoyVars, 0.004f);
            Start();
        }

        private void Start()
        {
            _out.StartTalking();
            _in.StartListening();
            _chart.StartTalking();
            _outcad.StartTalking();
            _rpi.StartTalking();
            _camera.StartTalking();
            _joy.StartListening();
        }

        public void Stop()
        {
            _out.Stop(); _in.Stop(); _chart.Stop(); _outcad.Stop();
            _rpi.Stop(); _camera.Stop(); _joy.Stop();
        }

        private void OnOutVars()
        {
            lock (_sc.DataLock)
            {
                var segs = new List<string>();
                foreach (var v in _sc.Variables)
                    if (v.Type != ShuffleVariable.ChartType)
                        segs.Add(v.Name + ";" + v.Value + ";" + v.Type + ";" + v.Direction);
                _out.OutString = segs.Count == 0 ? "null" : string.Join("&", segs);
            }
        }

        private void OnInVars()
        {
            string data = _in.OutString;
            if (data == "null" || string.IsNullOrEmpty(data)) return;
            lock (_sc.DataLock)
            {
                foreach (var item in data.Split('&'))
                {
                    var parts = item.Split(';');
                    if (parts.Length != 2) continue;
                    foreach (var v in _sc.Variables)
                        if (v.Name == parts[0]) { v.Value = parts[1]; break; }
                }
            }
        }

        private void OnChartVars()
        {
            lock (_sc.DataLock)
            {
                var segs = new List<string>();
                foreach (var v in _sc.Variables)
                    if (v.Type == ShuffleVariable.ChartType)
                        segs.Add(v.Name + ";" + v.Value);
                _chart.OutString = segs.Count == 0 ? "null" : string.Join("&", segs);
            }
        }

        private void OnOutcadVars()
        {
            lock (_sc.DataLock)
            {
                if (_sc.PrintArray.Count > 0)
                {
                    _outcad.OutString = string.Join("&", _sc.PrintArray);
                    _sc.PrintArray.Clear();
                }
                else _outcad.OutString = "null";
            }
        }

        private void OnRpiVars()
        {
            var ri = _robot.RobotInfo;
            string F(float v) => v.ToString(CultureInfo.InvariantCulture);
            _rpi.OutString = string.Join("&", new[]
            {
                F(ri.Temperature), F(ri.MemoryLoad), F(ri.CpuLoad), F(_robot.Power),
                F(ri.SpiTimeDev), F(ri.RxSpiTimeDev), F(ri.TxSpiTimeDev), F(ri.SpiCountDev),
                F(ri.ComTimeDev), F(ri.RxComTimeDev), F(ri.TxComTimeDev), F(ri.ComCountDev)
            });
        }

        private void OnCameraVars()
        {
            lock (_sc.DataLock)
            {
                if (_sc.CameraVariables.Count == 0)
                {
                    _camera.OutString = "null";
                    _camera.OutBytes = Encoding.UTF8.GetBytes("null");
                    return;
                }

                int requested;
                if (!int.TryParse(_camera.StrFromClient, out requested)) requested = -1;

                int idx;
                if (requested == -1)
                {
                    idx = _cameraToggler;
                    _cameraToggler = (_cameraToggler + 1) % _sc.CameraVariables.Count;
                }
                else idx = requested % _sc.CameraVariables.Count;

                var cur = _sc.CameraVariables[idx];
                _camera.OutString = cur.Name + ";" + cur.Width + ":" + cur.Height;
                _camera.OutBytes = cur.GetValue();
            }
        }

        private void OnJoyVars()
        {
            string data = _joy.OutString;
            if (data == "null" || string.IsNullOrEmpty(data)) return;
            lock (_sc.DataLock)
            {
                foreach (var item in data.Split('&'))
                {
                    var parts = item.Split(';');
                    if (parts.Length == 2 && int.TryParse(parts[1], out int val))
                    {
                        switch (parts[0])
                        {
                            case "A":
                                _sc._joystickData.BtnA = val == 1;
                                break;
                            case "X":
                                _sc._joystickData.BtnX = val == 1;
                                break;
                            case "Y":
                                _sc._joystickData.BtnY = val == 1;
                                break;
                            case "B":
                                _sc._joystickData.BtnB = val == 1;
                                break;
                            case "RightShoulder":
                                _sc._joystickData.RightShoulder = val == 1;
                                break;
                            case "LeftShoulder":
                                _sc._joystickData.LeftShoulder = val == 1;
                                break;
                            case "DPad_Up":
                                _sc._joystickData.DpudUp = val == 1;
                                break;
                            case "DPad_Down":
                                _sc._joystickData.DpudDown = val == 1;
                                break;
                            case "DPad_Right":
                                _sc._joystickData.DpudRight = val == 1;
                                break;
                            case "DPad_Left":
                                _sc._joystickData.DpudLeft = val == 1;
                                break;
                            case "LeftTrigger":
                                _sc._joystickData.LeftTrigger = (byte)val;
                                break;
                            case "RightTrigger":
                                _sc._joystickData.RightTrigger = (byte)val;
                                break;
                            case "LeftThumbstick_X":
                                _sc._joystickData.LeftStickX = val;
                                break;
                            case "LeftThumbstick_Y":
                                _sc._joystickData.LeftStickY = val;
                                break;
                            case "RightThumbstick_X":
                                _sc._joystickData.RightStickX = val;
                                break;
                            case "RightThumbstick_Y":
                                _sc._joystickData.LeftStickY = val;
                                break;
                        }
                    }
                }
            }
        }
    }
}
