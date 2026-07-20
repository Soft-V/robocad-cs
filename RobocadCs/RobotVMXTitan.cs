using OpenCvSharp;
using RobocadCs.Internal;
using RobocadCs.Internal.Common;

namespace RobocadCs
{
    public class RobotVMXTitan : Robot
    {
        private readonly StudicaInternal _internal;
        private float resetedYawVal = 0;

        public RobotVMXTitan(bool isRealRobot = false, DefaultStudicaConfiguration conf = null)
            : this(isRealRobot, (RobotConfiguration)(conf ?? new DefaultStudicaConfiguration())) { }

        private RobotVMXTitan(bool isRealRobot, RobotConfiguration conf)
            : base(isRealRobot, conf)
        {
            _internal = new StudicaInternal(this, conf);
            SignalHandler.Register(Stop);
        }

        public void Stop()
        {
            _internal.Stop();
            WriteLog("Program stopped");
        }

        public override void Dispose()
        {
            Stop();
            base.Dispose();
        }

        public float MotorSpeed0 { get => _internal.SpeedMotor0; set => _internal.SpeedMotor0 = value; }
        public float MotorSpeed1 { get => _internal.SpeedMotor1; set => _internal.SpeedMotor1 = value; }
        public float MotorSpeed2 { get => _internal.SpeedMotor2; set => _internal.SpeedMotor2 = value; }
        public float MotorSpeed3 { get => _internal.SpeedMotor3; set => _internal.SpeedMotor3 = value; }

        public int MotorEnc0 => _internal.EncMotor0;
        public int MotorEnc1 => _internal.EncMotor1;
        public int MotorEnc2 => _internal.EncMotor2;
        public int MotorEnc3 => _internal.EncMotor3;

        public float Yaw => RerangeAngle180(_internal.Yaw - resetedYawVal);
        public void ResetYaw() => resetedYawVal = Yaw;
        public float Us1 => _internal.Ultrasound1;
        public float Us2 => _internal.Ultrasound2;

        public int Analog1 => _internal.Analog1;
        public int Analog2 => _internal.Analog2;
        public int Analog3 => _internal.Analog3;
        public int Analog4 => _internal.Analog4;

        public bool[] TitanLimits => new[]
        {
            _internal.LimitH0, _internal.LimitL0, _internal.LimitH1, _internal.LimitL1,
            _internal.LimitH2, _internal.LimitL2, _internal.LimitH3, _internal.LimitL3
        };

        public bool[] VmxFlex => new[]
        {
            _internal.Flex0, _internal.Flex1, _internal.Flex2, _internal.Flex3,
            _internal.Flex4, _internal.Flex5, _internal.Flex6, _internal.Flex7
        };

        public Mat CameraImage => _internal.GetCamera();

        public void SetAngleHcdio(float value, int port) => _internal.SetServoAngle(value, port - 1);
        public void SetPwmHcdio(float value, int port) => _internal.SetServoPwm(value, port - 1);
        public void SetBoolHcdio(bool value, int port) => _internal.SetLedState(value, port - 1);

        private float RerangeAngle180(float angle)
        {
            while (angle > 180)
                angle -= 360;
            while (angle < -180)
                angle += 360;
            return angle;
        }
    }
}
