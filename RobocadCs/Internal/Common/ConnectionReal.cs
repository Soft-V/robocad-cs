using System;
using System.Diagnostics;
using System.Threading;
using OpenCvSharp;

namespace RobocadCs.Internal.Common
{
    public class ConnectionReal : ConnectionBase, IDisposable
    {
        private readonly Robot _robot;
        private readonly Updater _updater;
        private readonly RobotConfiguration _conf;
        private readonly LibHolder _lib;
        private VideoCapture _camera;
        private YDLidarX2 _lidar;
        private Thread _infoThread;

        public ConnectionReal(Robot robot, Updater updater, RobotConfiguration conf)
        {
            _robot = robot;
            _updater = updater;
            _conf = conf;
            _lib = new LibHolder(conf.LibHolderFirstPath);

            try
            {
                _camera = new VideoCapture(conf.CameraIndex);
                if (_camera.IsOpened())
                    _robot.WriteLog("Camera opened on index " + conf.CameraIndex);
                else
                    _robot.WriteLog("Camera FAILED to open on index " + conf.CameraIndex +
                                    " (VideoCapture did not throw, but device is not available)");
            }
            catch (Exception e)
            {
                _robot.WriteLog("Exception while creating camera instance: " + e.Message);
                if (e.InnerException != null)
                    _robot.WriteLog("  inner: " + e.InnerException.Message);
            }

            try
            {
                _lidar = new YDLidarX2(robot, conf.LidarPort);
                _lidar.Connect();
                _lidar.StartScan();
            }
            catch (Exception e)
            {
                _robot.WriteLog("Exception while creating lidar instance: " + e.Message);
            }

            if (conf.WithPiBlaster)
            {
                try
                {
                    Process.Start(new ProcessStartInfo("sudo", "/home/pi/pi-blaster/pi-blaster --pcm")
                    {
                        UseShellExecute = false
                    });
                }
                catch (Exception e) { _robot.WriteLog("pi-blaster start failed: " + e.Message); }
            }

            _infoThread = new Thread(_updater.RunUpdater) { IsBackground = true };
            _infoThread.Start();
        }

        public override void Stop()
        {
            if (_lidar != null)
            {
                _lidar.StopScan();
                _lidar.Disconnect();
            }
            _updater.StopRobotInfoThread = true;
            try { _infoThread?.Join(1000); } catch { }
            try { _camera?.Dispose(); } catch { }
            _lib?.Dispose();
        }

        // Returns the captured BGR Mat (owned by the caller — dispose it). null on failure.
        public override Mat GetCamera()
        {
            if (_camera == null) return null;
            try
            {
                var frame = new Mat();
                if (_camera.Read(frame) && !frame.Empty())
                    return frame;
                frame.Dispose();
            }
            catch (Exception e)
            {
                _robot.WriteLog("Camera read error: " + e.Message);
            }
            return null;
        }

        public override float[] GetLidar()
        {
            try { return _lidar?.GetData() ?? Array.Empty<float>(); }
            catch { return Array.Empty<float>(); }
        }

        public int SpiIni(string path, int channel, int speed, int mode) => _lib.InitSpi(path, channel, speed, mode);
        public int ComIni(string path, int baud) => _lib.InitUsb(path, baud);
        public byte[] SpiRw(byte[] data) => _lib.RwSpi(data);
        public byte[] ComRw(byte[] data) => _lib.RwUsb(data);
        public void SpiStop() => _lib.StopSpi();
        public void ComStop() => _lib.StopUsb();
        
        public void Dispose()
        {
            Stop();  
            GC.SuppressFinalize(this);
        }
    }
}
