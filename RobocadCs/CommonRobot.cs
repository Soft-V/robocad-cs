using RobocadCs.Internal;
using RobocadCs.Internal.Common;

namespace RobocadCs.Common
{
    public class CommonRobot : Robot
    {
        private readonly CommonRobotInternal _internal;

        public CommonRobot(bool isRealRobot = false, DefaultCommonConfiguration conf = null)
            : this(isRealRobot, (RobotConfiguration)(conf ?? new DefaultCommonConfiguration())) { }

        private CommonRobot(bool isRealRobot, RobotConfiguration conf)
            : base(isRealRobot, conf)
        {
            _internal = new CommonRobotInternal(this, (DefaultCommonConfiguration)conf);
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
        public float MotorSpeed4 { get => _internal.SpeedMotor4; set => _internal.SpeedMotor4 = value; }
        public float MotorSpeed5 { get => _internal.SpeedMotor5; set => _internal.SpeedMotor5 = value; }
        public float MotorSpeed6 { get => _internal.SpeedMotor6; set => _internal.SpeedMotor6 = value; }
        public float MotorSpeed7 { get => _internal.SpeedMotor7; set => _internal.SpeedMotor7 = value; }

        public int MotorEnc0 => _internal.EncMotor0;
        public int MotorEnc1 => _internal.EncMotor1;
        public int MotorEnc2 => _internal.EncMotor2;
        public int MotorEnc3 => _internal.EncMotor3;
        public int MotorEnc4 => _internal.EncMotor4;
        public int MotorEnc5 => _internal.EncMotor5;
        public int MotorEnc6 => _internal.EncMotor6;
        public int MotorEnc7 => _internal.EncMotor7;

        public float Yaw => _internal.Yaw;
        public float Us1 => _internal.Ultrasound1;
        public float Us2 => _internal.Ultrasound2;
        public float Us3 => _internal.Ultrasound3;
        public float Us4 => _internal.Ultrasound4;

        public int Analog1 => _internal.Analog1;
        public int Analog2 => _internal.Analog2;
        public int Analog3 => _internal.Analog3;
        public int Analog4 => _internal.Analog4;
        public int Analog5 => _internal.Analog5;
        public int Analog6 => _internal.Analog6;
        public int Analog7 => _internal.Analog7;
        public int Analog8 => _internal.Analog8;

        public bool[] Buttons => new[]
        {
            _internal.Button0, _internal.Button1, _internal.Button2, _internal.Button3,
            _internal.Button4, _internal.Button5, _internal.Button6, _internal.Button7
        };

        public bool Led0 { get => _internal.Led0; set => _internal.Led0 = value; }
        public bool Led1 { get => _internal.Led1; set => _internal.Led1 = value; }
        public bool Led2 { get => _internal.Led2; set => _internal.Led2 = value; }
        public bool Led3 { get => _internal.Led3; set => _internal.Led3 = value; }

        public CameraFrame CameraImage => _internal.GetCamera();

        public void SetAngleServo(float value, int port) => _internal.SetServoAngle(value, port - 1);
        public void SetPwmServo(float value, int port) => _internal.SetServoPwm(value, port - 1);
    }
}
