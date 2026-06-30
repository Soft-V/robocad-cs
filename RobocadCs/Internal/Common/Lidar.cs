using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;

namespace RobocadCs.Internal.Common
{
    public class YDLidarX2
    {
        private readonly Robot _robot;
        private readonly string _port;
        private readonly int _chunkSize;

        private const int MinRange = 10;
        private const int MaxRange = 8000;
        private const int MaxData = 20;
        private const int OutOfRange = 32768;

        private SerialPort _serial;
        private volatile bool _isConnected;
        private volatile bool _isScanning;
        private Thread _scanThread;
        private readonly object _lock = new object();

        private readonly uint[,] _distances = new uint[360, MaxData];
        private readonly int[] _result = new int[360];
        private readonly double[] _corrections = new double[8001];
        private byte[] _lastChunk = null;

        public YDLidarX2(Robot robot, string port, int chunkSize = 2000)
        {
            _robot = robot;
            _port = port;
            _chunkSize = chunkSize;

            for (int i = 0; i < 360; i++) _result[i] = OutOfRange;
            for (int a = 0; a < 360; a++)
                for (int d = 0; d < MaxData; d++) _distances[a, d] = OutOfRange;

            _corrections[0] = 0.0;
            for (int dist = 1; dist <= 8000; dist++)
                _corrections[dist] = Math.Atan(21.8 * ((155.3 - dist) / (155.3 * dist))) * (180.0 / Math.PI);
        }

        public bool Connect()
        {
            if (_isConnected) return true;
            try
            {
                _serial = new SerialPort(_port, 115200) { ReadTimeout = 1000 };
                _serial.Open();
                _isConnected = true;
            }
            catch (Exception e)
            {
                _robot.WriteLog("LiDAR: failed to open " + _port + ": " + e.Message);
                _isConnected = false;
            }
            return _isConnected;
        }

        public void Disconnect()
        {
            if (_isConnected)
            {
                try { _serial.Close(); } catch { }
                _isConnected = false;
            }
        }

        public bool StartScan()
        {
            if (!_isConnected) return false;
            _isScanning = true;
            _scanThread = new Thread(ScanLoop) { IsBackground = true };
            _scanThread.Start();
            return true;
        }

        public bool StopScan()
        {
            if (!_isScanning) return false;
            _isScanning = false;
            try { _scanThread?.Join(1000); } catch { }
            return true;
        }

        public float[] GetData()
        {
            lock (_lock)
            {
                float[] outArr = new float[360];
                for (int i = 0; i < 360; i++) outArr[i] = _result[i];
                return outArr;
            }
        }

        private void ScanLoop()
        {
            byte[] buf = new byte[_chunkSize];
            while (_isScanning)
            {
                int n;
                try { n = _serial.Read(buf, 0, _chunkSize); }
                catch (TimeoutException) { continue; }
                catch { continue; }
                if (n <= 0) continue;

                // prepend leftover, split on header 0xAA 0x55
                var data = new List<byte>((_lastChunk?.Length ?? 0) + n);
                if (_lastChunk != null) data.AddRange(_lastChunk);
                for (int i = 0; i < n; i++) data.Add(buf[i]);

                var packets = new List<byte[]>();
                var cur = new List<byte>();
                for (int i = 0; i < data.Count; i++)
                {
                    if (i + 1 < data.Count && data[i] == 0xAA && data[i + 1] == 0x55)
                    {
                        packets.Add(cur.ToArray());
                        cur.Clear();
                        i++; // skip 0x55
                    }
                    else cur.Add(data[i]);
                }
                _lastChunk = cur.ToArray(); // trailing fragment for next read

                int[] dpnt = new int[360];
                foreach (var p in packets) DecodePacket(p, dpnt);

                lock (_lock)
                {
                    for (int angle = 0; angle < 360; angle++)
                    {
                        if (dpnt[angle] == 0) _result[angle] = OutOfRange;
                        else
                        {
                            long sum = 0;
                            for (int k = 0; k < dpnt[angle]; k++) sum += _distances[angle, k];
                            _result[angle] = (int)(sum / dpnt[angle]);
                        }
                    }
                }
            }
        }

        private void DecodePacket(byte[] d, int[] dpnt)
        {
            int l = d.Length;
            if (l < 10) return;
            int sampleCnt = d[1];
            if (sampleCnt == 0) return;

            double startAngle = (BinaryPrimitives.ReadUInt16LittleEndian(d.AsSpan(2)) >> 1) / 64.0;
            double endAngle = (BinaryPrimitives.ReadUInt16LittleEndian(d.AsSpan(4)) >> 1) / 64.0;

            void Store(long dist, double angleF)
            {
                if (dist > MinRange)
                {
                    if (dist > MaxRange) dist = MaxRange;
                    long angle = (long)Math.Round(angleF + _corrections[dist]);
                    if (angle < 0) angle += 360;
                    if (angle >= 360) angle -= 360;
                    if (angle < 0 || angle >= 360) return;
                    int pnt = dpnt[angle];
                    if (pnt < MaxData)
                    {
                        _distances[angle, pnt] = (uint)dist;
                        if (pnt < MaxData - 1) dpnt[angle] = pnt + 1;
                    }
                }
            }

            if (sampleCnt == 1)
            {
                long dist = (long)Math.Round(BinaryPrimitives.ReadUInt16LittleEndian(d.AsSpan(8)) / 4.0);
                Store(dist, startAngle);
            }
            else
            {
                if (startAngle == endAngle) return;
                if (l != 8 + 2 * sampleCnt) return;

                double step = endAngle < startAngle
                    ? (endAngle + 360.0 - startAngle) / (sampleCnt - 1)
                    : (endAngle - startAngle) / (sampleCnt - 1);

                int p = 8;
                double cur = startAngle;
                while (p + 1 < l)
                {
                    long dist = (long)Math.Round(BinaryPrimitives.ReadUInt16LittleEndian(d.AsSpan(p)) / 4.0);
                    Store(dist, cur);
                    cur += step;
                    if (cur >= 360.0) cur -= 360.0;
                    p += 2;
                }
            }
        }
    }
}
