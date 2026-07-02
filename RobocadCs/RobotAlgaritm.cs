using System;
using RobocadCs.Internal;
using RobocadCs.Internal.Common;

namespace RobocadCs
{
    public class RobotAlgaritm : Robot
    {
        private readonly AlgaritmInternal _internal;

        public RobotAlgaritm(bool isRealRobot = false, DefaultAlgaritmConfiguration conf = null)
            : this(isRealRobot, (RobotConfiguration)(conf ?? new DefaultAlgaritmConfiguration())) { }

        private RobotAlgaritm(bool isRealRobot, RobotConfiguration conf)
            : base(isRealRobot, conf)
        {
            _internal = new AlgaritmInternal(this, conf);
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

        public float Yaw => _internal.Yaw;
        public float Pitch => _internal.Pitch;
        public float Roll => _internal.Roll;

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

        public bool[] Inputs => (bool[])_internal.Inputs.Clone();
        public bool[] Outputs => (bool[])_internal.Outputs.Clone();
        public void SetOutput(bool value, int port) => _internal.SetOutput(port - 1, value);

        public float AdditionalServo1 { get => _internal.AdditionalServo1; set => _internal.AdditionalServo1 = value; }
        public float AdditionalServo2 { get => _internal.AdditionalServo2; set => _internal.AdditionalServo2 = value; }

        public bool IsStep1Busy => _internal.IsStep1Busy;
        public bool IsStep2Busy => _internal.IsStep2Busy;
        public void StepMotorMove(int num, int steps, int stepsPerSecond, bool direction)
            => _internal.StepMotorMove(num, steps, stepsPerSecond, direction);

        public void SetPidSettings(bool usePid, float p, float i, float d)
        {
            _internal.UsePid = usePid; _internal.PPid = p; _internal.IPid = i; _internal.DPid = d;
        }

        public bool[] TitanLimits => new[]
        {
            _internal.LimitH0, _internal.LimitL0, _internal.LimitH1, _internal.LimitL1,
            _internal.LimitH2, _internal.LimitL2, _internal.LimitH3, _internal.LimitL3
        };

        public CameraFrame CameraImage => _internal.GetCamera();
        public float[] LidarData => _internal.GetLidar();

        public void SetAngleServo(float value, int port) => _internal.SetServoAngle(value, port - 1);
    }
}
