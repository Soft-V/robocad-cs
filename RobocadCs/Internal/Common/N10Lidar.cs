using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;

namespace RobocadCs.Internal.Common
{
    // LDROBOT N10. Packet: AA-style header 0xA5 0x5A, 58 bytes, big-endian fields.
    //  [0..1] header, [5..6] start angle, [7..54] 16 points x 3 bytes (2 distance + 1 intensity),
    //  [55..56] end angle. Angles are in hundredths of a degree.
    public class N10Lidar : LidarBase
    {
        private const byte PkgHeader0 = 0xA5;
        private const byte PkgHeader1 = 0x5A;
        private const int MinPayload = 58;
        private const int PointPerPack = 16;
        private const int DefaultBaud = 230400;

        // Drop the backlog if parsing falls this far behind the sensor.
        private const int MaxBufferedPackets = 100;

        private readonly Robot _robot;
        private readonly string _port;
        private readonly int _baud;

        private SerialPort _serial;
        private volatile bool _shutdown;
        private Thread _scanThread;

        private readonly object _lock = new object();
        private readonly float[] _data = new float[360];
        private readonly List<byte> _buffer = new List<byte>();

        public N10Lidar(Robot robot, string port, int baud = DefaultBaud)
        {
            _robot = robot;
            _port = port;
            _baud = baud;
        }

        public override void Start()
        {
            try
            {
                _serial = new SerialPort(_port, _baud) { ReadTimeout = 1000 };
                _serial.Open();
            }
            catch (Exception e)
            {
                _robot.WriteLog("LiDAR N10: failed to open " + _port + ": " + e.Message);
                return;
            }

            _shutdown = false;
            _scanThread = new Thread(ScanLoop) { IsBackground = true };
            _scanThread.Start();
        }

        public override void Stop()
        {
            _shutdown = true;
            try { _scanThread?.Join(1000); } catch { }
            try { _serial?.Close(); } catch { }
        }

        public override float[] GetData()
        {
            lock (_lock) { return (float[])_data.Clone(); }
        }

        private void ScanLoop()
        {
            byte[] buf = new byte[MinPayload * 16];
            while (!_shutdown)
            {
                int n;
                try { n = _serial.Read(buf, 0, buf.Length); }
                catch (TimeoutException) { continue; }
                catch { continue; }
                if (n <= 0) continue;

                for (int i = 0; i < n; i++) _buffer.Add(buf[i]);
                if (_buffer.Count > MinPayload * MaxBufferedPackets) _buffer.Clear();

                ParseBuffer();
            }
        }

        private void ParseBuffer()
        {
            while (true)
            {
                int start = -1;
                for (int i = 0; i + 1 < _buffer.Count; i++)
                {
                    if (_buffer[i] == PkgHeader0 && _buffer[i + 1] == PkgHeader1) { start = i; break; }
                }

                if (start < 0)
                {
                    // No header yet. Keep the last byte: it may be a header split across reads.
                    if (_buffer.Count > 1) _buffer.RemoveRange(0, _buffer.Count - 1);
                    return;
                }

                if (start > 0) _buffer.RemoveRange(0, start);
                if (_buffer.Count < MinPayload) return;

                DecodePacket();
                _buffer.RemoveRange(0, MinPayload);
            }
        }

        private void DecodePacket()
        {
            int startAngle = _buffer[5] * 256 + _buffer[6];
            int endAngle = _buffer[55] * 256 + _buffer[56];

            double step = ((endAngle + 36000 - startAngle) % 36000) / (double)(PointPerPack - 1) / 100.0;
            double startDeg = startAngle / 100.0;

            lock (_lock)
            {
                for (int i = 0; i < PointPerPack; i++)
                {
                    int o = 7 + i * 3;
                    int dist = _buffer[o] * 256 + _buffer[o + 1];

                    int angle = (int)Math.Round(startDeg + step * i) % 360;
                    if (angle < 0) angle += 360;

                    _data[angle] = dist;
                }
            }
        }
    }
}
