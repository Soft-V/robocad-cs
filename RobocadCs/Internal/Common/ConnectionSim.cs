using System;

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

        public override CameraFrame GetCamera()
        {
            byte[] data = _cameraChannel.GetBytesSafe();
            if (data.Length != CameraBytes) return null;

            // incoming stream is RGB; convert to BGR
            byte[] bgr = new byte[CameraBytes];
            for (int i = 0; i < CameraBytes; i += 3)
            {
                bgr[i] = data[i + 2];
                bgr[i + 1] = data[i + 1];
                bgr[i + 2] = data[i];
            }
            return new CameraFrame(640, 480, bgr);
        }

        public override float[] GetLidar() => Array.Empty<float>();

        public void SetData(byte[] data) => _talkChannel.SetBytesSafe(data);

        public byte[] GetData() => _listenChannel.GetBytesSafe();
    }
}
