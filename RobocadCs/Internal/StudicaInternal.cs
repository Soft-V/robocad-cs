using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using RobocadCs.Internal.Common;

namespace RobocadCs.Internal
{
    public class StudicaInternal : RobotInternal
    {
        public static readonly int[] HcdioConstArray = { 4, 18, 17, 27, 23, 22, 24, 25, 7, 5 };
        public readonly float[] HcdioValues = new float[10];

        public StudicaInternal(Robot robot, RobotConfiguration conf) : base(robot, conf)
        {
        }

        public int RawEncMotor0 { get; set; }
        public int RawEncMotor1 { get; set; }
        public int RawEncMotor2 { get; set; }
        public int RawEncMotor3 { get; set; }

        public bool Flex0 { get; set; }
        public bool Flex1 { get; set; }
        public bool Flex2 { get; set; }
        public bool Flex3 { get; set; }
        public bool Flex4 { get; set; }
        public bool Flex5 { get; set; }
        public bool Flex6 { get; set; }
        public bool Flex7 { get; set; }

        public void SetServoAngle(float angle, int pin)
        {
            double dut = 0.000666 * angle + 0.05;
            HcdioValues[pin] = (float)dut;
            EchoToFile(HcdioConstArray[pin] + "=" + dut);
        }

        public void SetLedState(bool state, int pin)
        {
            HcdioValues[pin] = state ? 0.2f : 0.0f;
            EchoToFile(HcdioConstArray[pin] + "=" + (state ? 0.2f : 0.0f).ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        public void SetServoPwm(float pwm, int pin)
        {
            HcdioValues[pin] = pwm;
            EchoToFile(HcdioConstArray[pin] + "=" + pwm);
        }

        public void DisableServo(int pin)
        {
            HcdioValues[pin] = 0.0f;
            EchoToFile(HcdioConstArray[pin] + "=0.0");
        }

        private void EchoToFile(string s)
        {
            if (!_robot.OnRealRobot) return;
            try
            {
                using (var f = new StreamWriter("/dev/pi-blaster", append: true))
                    f.WriteLine(s);
            }
            catch
            {
            }
        }

        protected override TitanCom CreateTitanCom() => new TitanComStudica();
        protected override VmxSpi CreateVmxSpi() => new VmxSpiStudica();
        protected override RobocadConnection CreateRobocadConnection() => new RobocadConnectionStudica();

        private class RobocadConnectionStudica : RobocadConnection
        {
            private StudicaInternal Ri => (StudicaInternal)_ri;

            protected override void Loop()
            {
                while (!_stop)
                {
                    // TX: 14 floats (speeds[4] + hcdio[10]) = 56 bytes
                    var w = new ByteWriter(56);
                    w.WriteFloat(Ri.SpeedMotor0);
                    w.WriteFloat(Ri.SpeedMotor1);
                    w.WriteFloat(Ri.SpeedMotor2);
                    w.WriteFloat(Ri.SpeedMotor3);
                    for (int i = 0; i < 10; i++) w.WriteFloat(Ri.HcdioValues[i]);
                    _conn.SetData(w.ToArray());

                    // RX: <4i2f4Hf16B = 52 bytes
                    byte[] d = _conn.GetData();
                    if (d.Length >= 52)
                    {
                        var r = new ByteReader(d);
                        Ri.EncMotor0 = r.ReadInt32();
                        Ri.EncMotor1 = r.ReadInt32();
                        Ri.EncMotor2 = r.ReadInt32();
                        Ri.EncMotor3 = r.ReadInt32();
                        Ri.Ultrasound1 = r.ReadFloat();
                        Ri.Ultrasound2 = r.ReadFloat();
                        Ri.Analog1 = r.ReadUInt16();
                        Ri.Analog2 = r.ReadUInt16();
                        Ri.Analog3 = r.ReadUInt16();
                        Ri.Analog4 = r.ReadUInt16();
                        Ri.Yaw = r.ReadFloat();
                        Ri.LimitH0 = r.ReadByte() == 1;
                        Ri.LimitL0 = r.ReadByte() == 1;
                        Ri.LimitH1 = r.ReadByte() == 1;
                        Ri.LimitL1 = r.ReadByte() == 1;
                        Ri.LimitH2 = r.ReadByte() == 1;
                        Ri.LimitL2 = r.ReadByte() == 1;
                        Ri.LimitH3 = r.ReadByte() == 1;
                        Ri.LimitL3 = r.ReadByte() == 1;
                        Ri.Flex0 = r.ReadByte() == 1;
                        Ri.Flex1 = r.ReadByte() == 1;
                        Ri.Flex2 = r.ReadByte() == 1;
                        Ri.Flex3 = r.ReadByte() == 1;
                        Ri.Flex4 = r.ReadByte() == 1;
                        Ri.Flex5 = r.ReadByte() == 1;
                        Ri.Flex6 = r.ReadByte() == 1;
                        Ri.Flex7 = r.ReadByte() == 1;
                    }

                    Thread.Sleep(4);
                }
            }
        }

        private class TitanComStudica : TitanCom
        {
            private StudicaInternal Ri => (StudicaInternal)_ri;
            
            protected override void ParseRx(byte[] data)
            {
                if (data.Length < 43) return;
                if (data[42] != 33)
                {
                    if (data[0] == 1 && data[24] == 111)
                    {
                        int e0 = (data[2] << 8) | data[1];
                        int e1 = (data[4] << 8) | data[3];
                        int e2 = (data[6] << 8) | data[5];
                        int e3 = (data[8] << 8) | data[7];
                        SetEncoders(e0, e1, e2, e3);

                        Ri.LimitL0 = Funcad.AccessBit(data[9], 1);
                        Ri.LimitH0 = Funcad.AccessBit(data[9], 2);
                        Ri.LimitL1 = Funcad.AccessBit(data[9], 3);
                        Ri.LimitH1 = Funcad.AccessBit(data[9], 4);
                        Ri.LimitL2 = Funcad.AccessBit(data[9], 5);
                        Ri.LimitH2 = Funcad.AccessBit(data[9], 6);
                        Ri.LimitL3 = Funcad.AccessBit(data[10], 1);
                        Ri.LimitH3 = Funcad.AccessBit(data[10], 2);
                    }
                }
                else
                {
                    _robot.WriteLog("received wrong data");
                }
            }

            protected override byte[] BuildTx()
            {
                byte[] tx = new byte[48];
                tx[0] = 1;

                void PackSpeed(double speed, int idxHigh, int idxLow)
                {
                    ushort val = (ushort)Math.Abs(speed / 100.0 * 65535.0);
                    tx[idxHigh] = (byte)((val >> 8) & 0xFF);
                    tx[idxLow] = (byte)(val & 0xFF);
                }

                PackSpeed(Ri.SpeedMotor0, 2, 3);
                PackSpeed(Ri.SpeedMotor1, 4, 5);
                PackSpeed(Ri.SpeedMotor2, 6, 7);
                PackSpeed(Ri.SpeedMotor3, 8, 9);

                byte dir = 0b10000001;
                if (Ri.SpeedMotor0 >= 0) dir |= (1 << 6);
                if (Ri.SpeedMotor1 >= 0) dir |= (1 << 5);
                if (Ri.SpeedMotor2 >= 0) dir |= (1 << 4);
                if (Ri.SpeedMotor3 >= 0) dir |= (1 << 3);
                tx[10] = dir;

                tx[11] = 0b10100001; // third bit is ProgramIsRunning
                tx[20] = 222;
                return tx;
            }

            private void SetEncoders(int e0, int e1, int e2, int e3)
            {
                Ri.EncMotor0 -= NormalDiff(e0, Ri.RawEncMotor0);
                Ri.EncMotor1 -= NormalDiff(e1, Ri.RawEncMotor1);
                Ri.EncMotor2 -= NormalDiff(e2, Ri.RawEncMotor2);
                Ri.EncMotor3 -= NormalDiff(e3, Ri.RawEncMotor3);
                Ri.RawEncMotor0 = e0;
                Ri.RawEncMotor1 = e1;
                Ri.RawEncMotor2 = e2;
                Ri.RawEncMotor3 = e3;
            }

            private static int NormalDiff(int curr, int last)
            {
                int diff = curr - last;
                if (diff > 30000) diff = -(last + (65535 - curr));
                else if (diff < -30000) diff = curr + (65535 - last);
                return diff;
            }
        }

        private class VmxSpiStudica : VmxSpi
        {
            private StudicaInternal Ri => (StudicaInternal)_ri; 
            protected override void ParseRx(byte[] data)
            {
                if (data.Length == 0) return;
                if (data[0] == 1)
                {
                    int yawUi = (data[2] << 8) | data[1];
                    int us1 = (data[4] << 8) | data[3];
                    Ri.Ultrasound1 = us1 / 100.0f;
                    int us2 = (data[6] << 8) | data[5];
                    Ri.Ultrasound2 = us2 / 100.0f;

                    float power = ((data[8] << 8) | data[7]) / 100.0f;
                    _robot.Power = power;

                    float newYaw = (yawUi / 100.0f) * (Funcad.AccessBit(data[9], 1) ? 1f : -1f);
                    CalcYawUnlim(newYaw, Ri.Yaw);
                    Ri.Yaw = newYaw;

                    Ri.Flex0 = Funcad.AccessBit(data[9], 2);
                    Ri.Flex1 = Funcad.AccessBit(data[9], 3);
                    Ri.Flex2 = Funcad.AccessBit(data[9], 4);
                    Ri.Flex3 = Funcad.AccessBit(data[9], 5);
                    Ri.Flex4 = Funcad.AccessBit(data[9], 6);
                }
                else if (data[0] == 2)
                {
                    Ri.Analog1 = (data[2] << 8) | data[1];
                    Ri.Analog2 = (data[4] << 8) | data[3];
                    Ri.Analog3 = (data[6] << 8) | data[5];
                    Ri.Analog4 = (data[8] << 8) | data[7];
                    Ri.Flex5 = Funcad.AccessBit(data[9], 1);
                    Ri.Flex6 = Funcad.AccessBit(data[9], 2);
                    Ri.Flex7 = Funcad.AccessBit(data[9], 3);
                }
            }

            protected override byte[] BuildTx()
            {
                byte[] tx = new byte[10];
                if (_toggler == 0)
                {
                    tx[0] = 1;
                    tx[9] = 222;
                }

                return tx;
            }

            private void CalcYawUnlim(float newYaw, float oldYaw)
            {
                float delta = newYaw - oldYaw;
                if (delta < -180f) delta = (180f - oldYaw) + (180f + newYaw);
                else if (delta > 180f) delta = -(180f + oldYaw) - (180f - newYaw);
                Ri.YawUnlim += delta;
            }
        }
    }
}