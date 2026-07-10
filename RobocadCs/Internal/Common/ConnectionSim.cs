using System;
using System.Runtime.InteropServices;
using OpenCvSharp;

namespace RobocadCs.Internal.Common
{
    public class ConnectionSim : ConnectionBase
    {
        private const int PortSetData = 65431;
        private const int PortGetData = 65432;
        private const int PortCamera = 65438;
        private const int CameraBytes = 640 * 480 * 3; // 921600

        private readonly Robot _robot;
        private readonly TalkPort _talkChannel;
        private readonly ListenPort _listenChannel;
        private readonly ListenPort _cameraChannel;

        public ConnectionSim(Robot robot)
        {
            _robot = robot;
            _talkChannel = new TalkPort(robot, PortSetData);
            _talkChannel.Start();
            _listenChannel = new ListenPort(robot, PortGetData);
            _listenChannel.Start();
            _cameraChannel = new ListenPort(robot, PortCamera);
            _cameraChannel.Start();
        }

        public override void Stop()
        {
            _talkChannel.Stop();
            _listenChannel.Stop();
            _cameraChannel.Stop();
        }

        // Returns a new BGR Mat (owned by the caller — dispose it). null if no frame yet.
        public override Mat GetCamera()
        {
            byte[] data = _cameraChannel.GetBytesSafe();
            if (data.Length != CameraBytes) return null;

            // incoming stream is RGB 640x480; convert to BGR and flip vertically
            var frame = new Mat(480, 640, MatType.CV_8UC3);
            Marshal.Copy(data, 0, frame.Data, data.Length);
            Cv2.CvtColor(frame, frame, ColorConversionCodes.RGB2BGR);
            Cv2.Flip(frame, frame, FlipMode.X);
            return frame;
        }

        public override float[] GetLidar() => Array.Empty<float>();

        public void SetData(byte[] data) => _talkChannel.SetBytesSafe(data);

        public byte[] GetData() => _listenChannel.GetBytesSafe();
    }
}
