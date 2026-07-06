using System;
using System.Threading;
using RobocadCs.Internal.Common;

namespace RobocadCs.Internal
{
    public class CommonRobotInternal
    {
        public float SpeedMotor0 { get; set; }
        public float SpeedMotor1 { get; set; }
        public float SpeedMotor2 { get; set; }
        public float SpeedMotor3 { get; set; }
        public float SpeedMotor4 { get; set; }
        public float SpeedMotor5 { get; set; }
        public float SpeedMotor6 { get; set; }
        public float SpeedMotor7 { get; set; }
        public int EncMotor0 { get; private set; }
        public int EncMotor1 { get; private set; }
        public int EncMotor2 { get; private set; }
        public int EncMotor3 { get; private set; }
        public int EncMotor4 { get; private set; }
        public int EncMotor5 { get; private set; }
        public int EncMotor6 { get; private set; }
        public int EncMotor7 { get; private set; }
        public bool Button0 { get; private set; }
        public bool Button1 { get; private set; }
        public bool Button2 { get; private set; }
        public bool Button3 { get; private set; }
        public bool Button4 { get; private set; }
        public bool Button5 { get; private set; }
        public bool Button6 { get; private set; }
        public bool Button7 { get; private set; }

        public float Yaw { get; private set; }
        public float Ultrasound1 { get; private set; }
        public float Ultrasound2 { get; private set; }
        public float Ultrasound3 { get; private set; }
        public float Ultrasound4 { get; private set; }
        public int Analog1 { get; private set; }
        public int Analog2 { get; private set; }
        public int Analog3 { get; private set; }
        public int Analog4 { get; private set; }
        public int Analog5 { get; private set; }
        public int Analog6 { get; private set; }
        public int Analog7 { get; private set; }
        public int Analog8 { get; private set; }
        public bool Led0 { get; set; }
        public bool Led1 { get; set; }
        public bool Led2 { get; set; }
        public bool Led3 { get; set; }
        private readonly float[] ServoValues = new float[10];

        private readonly ConnectionSim _connection;
        private readonly RobocadConnection _robocad;

        public CommonRobotInternal(Robot robot, DefaultCommonConfiguration conf)
        {
            if (robot.OnRealRobot)
                throw new InvalidOperationException("CommonRobot could only be used in simulator");

            _connection = new ConnectionSim(robot);
            _robocad = new RobocadConnection();
            _robocad.Start(_connection, robot, this);
        }

        public void Stop()
        {
            _robocad?.Stop();
            _connection?.Stop();
        }

        public CameraFrame GetCamera() => _connection.GetCamera();

        public void SetServoAngle(float angle, int pin) => ServoValues[pin] = (float)(0.000666 * angle + 0.05);
        public void SetServoPwm(float pwm, int pin) => ServoValues[pin] = pwm;
        public void DisableServo(int pin) => ServoValues[pin] = 0.0f;

        private class RobocadConnection
        {
            private Thread _thread;
            private volatile bool _stop;
            private ConnectionSim _conn;
            private CommonRobotInternal _ri;

            public void Start(ConnectionSim conn, Robot robot, CommonRobotInternal ri)
            {
                _conn = conn;
                _ri = ri;
                robot.Power = 12.0f;
                _stop = false;
                _thread = new Thread(Loop) { IsBackground = true };
                _thread.Start();
            }

            public void Stop()
            {
                _stop = true;
                try
                {
                    _thread?.Join(1000);
                }
                catch { }
            }

            private byte[] BuildTx()
            {
                var w = new ByteWriter(88);
                w.WriteFloat(_ri.SpeedMotor0);
                w.WriteFloat(_ri.SpeedMotor1);
                w.WriteFloat(_ri.SpeedMotor2);
                w.WriteFloat(_ri.SpeedMotor3);
                w.WriteFloat(_ri.SpeedMotor4);
                w.WriteFloat(_ri.SpeedMotor5);
                w.WriteFloat(_ri.SpeedMotor6);
                w.WriteFloat(_ri.SpeedMotor7);
                for (int i = 0; i < 10; i++) w.WriteFloat(_ri.ServoValues[i]);
                w.WriteFloat(_ri.Led0 ? 1f : 0f);
                w.WriteFloat(_ri.Led1 ? 1f : 0f);
                w.WriteFloat(_ri.Led2 ? 1f : 0f);
                w.WriteFloat(_ri.Led3 ? 1f : 0f);
                return w.ToArray();
            }

            private void Loop()
            {
                while (!_stop)
                {
                    _conn.SetData(BuildTx());

                    byte[] d = _conn.GetData();
                    if (d.Length >= 76)
                    {
                        var r = new ByteReader(d);
                        _ri.EncMotor0 = r.ReadInt32();
                        _ri.EncMotor1 = r.ReadInt32();
                        _ri.EncMotor2 = r.ReadInt32();
                        _ri.EncMotor3 = r.ReadInt32();
                        _ri.EncMotor4 = r.ReadInt32();
                        _ri.EncMotor5 = r.ReadInt32();
                        _ri.EncMotor6 = r.ReadInt32();
                        _ri.EncMotor7 = r.ReadInt32();
                        _ri.Ultrasound1 = r.ReadFloat();
                        _ri.Ultrasound2 = r.ReadFloat();
                        _ri.Ultrasound3 = r.ReadFloat();
                        _ri.Ultrasound4 = r.ReadFloat();
                        _ri.Analog1 = r.ReadUInt16();
                        _ri.Analog2 = r.ReadUInt16();
                        _ri.Analog3 = r.ReadUInt16();
                        _ri.Analog4 = r.ReadUInt16();
                        _ri.Analog5 = r.ReadUInt16();
                        _ri.Analog6 = r.ReadUInt16();
                        _ri.Analog7 = r.ReadUInt16();
                        _ri.Analog8 = r.ReadUInt16();
                        _ri.Yaw = r.ReadFloat();
                        _ri.Button0 = r.ReadByte() == 1;
                        _ri.Button1 = r.ReadByte() == 1;
                        _ri.Button2 = r.ReadByte() == 1;
                        _ri.Button3 = r.ReadByte() == 1;
                        _ri.Button4 = r.ReadByte() == 1;
                        _ri.Button5 = r.ReadByte() == 1;
                        _ri.Button6 = r.ReadByte() == 1;
                        _ri.Button7 = r.ReadByte() == 1;
                    }

                    Thread.Sleep(4);
                }
            }
        }
    }
}