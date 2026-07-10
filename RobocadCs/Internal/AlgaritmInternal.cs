using System;
using System.Diagnostics;
using System.Threading;
using RobocadCs.Internal.Common;

namespace RobocadCs.Internal
{
    public class AlgaritmInternal : RobotInternal
    {
        public AlgaritmInternal(Robot robot, RobotConfiguration conf) : base(robot, conf)
        {
        }
        
        public void SetServoAngle(float angle, int pin) => ServoAngles[pin] = angle;

        public void StepMotorMove(int num, int steps, int stepsPerSecond, bool direction)
        {
            if (num == 1)
            {
                StepMotor1Steps = steps; StepMotor1StepsPerS = stepsPerSecond; StepMotor1Direction = direction;
            }
            else if (num == 2)
            {
                StepMotor2Steps = steps; StepMotor2StepsPerS = stepsPerSecond; StepMotor2Direction = direction;
            }
        }
        
        protected override TitanCom CreateTitanCom() => new TitanComAlgaritm();
        protected override VmxSpi CreateVmxSpi() => new VmxSpiAlgaritm();
        protected override RobocadConnection CreateRobocadConnection() => new RobocadConnectionAlgaritm();

        private class RobocadConnectionAlgaritm : RobocadConnection
        {
            // TX: 28 floats (112 bytes)
            protected override byte[] BuildTx()
            {
                var w = new ByteWriter(112);
                w.WriteFloat(_ri.SpeedMotor0); w.WriteFloat(_ri.SpeedMotor1);
                w.WriteFloat(_ri.SpeedMotor2); w.WriteFloat(_ri.SpeedMotor3);
                for (int i = 0; i < 8; i++) w.WriteFloat(_ri.ServoAngles[i]);
                w.WriteFloat(_ri.AdditionalServo1); w.WriteFloat(_ri.AdditionalServo2);
                w.WriteFloat(_ri.StepMotor1Steps); w.WriteFloat(_ri.StepMotor2Steps);
                w.WriteFloat(_ri.StepMotor1StepsPerS); w.WriteFloat(_ri.StepMotor2StepsPerS);
                w.WriteFloat(_ri.StepMotor1Direction ? 1f : 0f);
                w.WriteFloat(_ri.StepMotor2Direction ? 1f : 0f);
                w.WriteFloat(_ri.UsePid ? 1f : 0f);
                w.WriteFloat(_ri.PPid); w.WriteFloat(_ri.IPid); w.WriteFloat(_ri.DPid);
                for (int i = 0; i < 4; i++) w.WriteFloat(_ri.Outputs[i] ? 1f : 0f);
                return w.ToArray();
            }

            protected override void Loop()
            {
                while (!_stop)
                {
                    _conn.SetData(BuildTx());

                    // RX: <4i4f8H3f14B = 74 bytes
                    byte[] d = _conn.GetData();
                    if (d.Length >= 74)
                    {
                        var r = new ByteReader(d);
                        _ri.EncMotor0 = r.ReadInt32(); _ri.EncMotor1 = r.ReadInt32();
                        _ri.EncMotor2 = r.ReadInt32(); _ri.EncMotor3 = r.ReadInt32();
                        _ri.Ultrasound1 = r.ReadFloat(); _ri.Ultrasound2 = r.ReadFloat();
                        _ri.Ultrasound3 = r.ReadFloat(); _ri.Ultrasound4 = r.ReadFloat();
                        _ri.Analog1 = r.ReadUInt16(); _ri.Analog2 = r.ReadUInt16();
                        _ri.Analog3 = r.ReadUInt16(); _ri.Analog4 = r.ReadUInt16();
                        _ri.Analog5 = r.ReadUInt16(); _ri.Analog6 = r.ReadUInt16();
                        _ri.Analog7 = r.ReadUInt16(); _ri.Analog8 = r.ReadUInt16();
                        _ri.Yaw = r.ReadFloat(); _ri.Pitch = r.ReadFloat(); _ri.Roll = r.ReadFloat();
                        _ri.LimitH0 = r.ReadByte() == 1; _ri.LimitL0 = r.ReadByte() == 1;
                        _ri.LimitH1 = r.ReadByte() == 1; _ri.LimitL1 = r.ReadByte() == 1;
                        _ri.LimitH2 = r.ReadByte() == 1; _ri.LimitL2 = r.ReadByte() == 1;
                        _ri.LimitH3 = r.ReadByte() == 1; _ri.LimitL3 = r.ReadByte() == 1;
                        _ri.Inputs[0] = r.ReadByte() == 1; _ri.Inputs[1] = r.ReadByte() == 1;
                        _ri.Inputs[2] = r.ReadByte() == 1; _ri.Inputs[3] = r.ReadByte() == 1;
                        _ri.IsStep1Busy = r.ReadByte() == 1; _ri.IsStep2Busy = r.ReadByte() == 1;
                    }
                    Thread.Sleep(4);
                }
            }
        }

        private class TitanComAlgaritm : TitanCom
        {
            protected override void ParseRx(byte[] data)
            {
                if (data.Length < 41) return;
                if (data[0] == 1 && data[40] == 222)
                {
                    _ri.EncMotor0 = (data[4] << 24) | (data[3] << 16) | (data[2] << 8) | data[1];
                    _ri.EncMotor1 = (data[8] << 24) | (data[7] << 16) | (data[6] << 8) | data[5];
                    _ri.EncMotor2 = (data[12] << 24) | (data[11] << 16) | (data[10] << 8) | data[9];
                    _ri.EncMotor3 = (data[16] << 24) | (data[15] << 16) | (data[14] << 8) | data[13];

                    _ri.LimitL0 = Funcad.AccessBit(data[17], 7);
                    _ri.LimitH0 = Funcad.AccessBit(data[17], 6);
                    _ri.LimitL1 = Funcad.AccessBit(data[17], 5);
                    _ri.LimitH1 = Funcad.AccessBit(data[17], 4);
                    _ri.LimitL2 = Funcad.AccessBit(data[17], 3);
                    _ri.LimitH2 = Funcad.AccessBit(data[17], 2);
                    _ri.LimitL3 = Funcad.AccessBit(data[17], 1);
                    _ri.LimitH3 = Funcad.AccessBit(data[17], 0);

                    _ri.IsStep1Busy = data[18] != 0;
                    _ri.IsStep2Busy = data[19] != 0;
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

                tx[1] = (byte)(sbyte)Clamp(_ri.SpeedMotor0);
                tx[2] = (byte)(sbyte)Clamp(_ri.SpeedMotor1);
                tx[3] = (byte)(sbyte)Clamp(_ri.SpeedMotor2);
                tx[4] = (byte)(sbyte)Clamp(_ri.SpeedMotor3);

                byte dir = 0b11000001;
                if (_ri.StepMotor1Direction) dir |= (1 << 5);
                if (_ri.StepMotor2Direction) dir |= (1 << 4);
                if (_ri.UsePid) dir |= (1 << 3);
                tx[5] = dir;

                tx[6] = (byte)_ri.AdditionalServo1;
                tx[7] = (byte)_ri.AdditionalServo2;

                WriteInt32Be(tx, 8, Math.Abs(_ri.StepMotor1Steps));
                WriteInt32Be(tx, 12, Math.Abs(_ri.StepMotor2Steps));
                WriteInt32Be(tx, 16, Math.Abs(_ri.StepMotor1StepsPerS));
                WriteInt32Be(tx, 20, Math.Abs(_ri.StepMotor2StepsPerS));

                WriteFloatLe(tx, 24, _ri.PPid);
                WriteFloatLe(tx, 28, _ri.IPid);
                WriteFloatLe(tx, 32, _ri.DPid);

                tx[40] = 222;
                return tx;
            }

            private static int Clamp(float v) => (int)Math.Max(-100f, Math.Min(100f, v));

            private static void WriteInt32Be(byte[] buf, int idx, int value)
            {
                uint u = (uint)value;
                buf[idx] = (byte)((u >> 24) & 0xFF);
                buf[idx + 1] = (byte)((u >> 16) & 0xFF);
                buf[idx + 2] = (byte)((u >> 8) & 0xFF);
                buf[idx + 3] = (byte)(u & 0xFF);
            }

            private static void WriteFloatLe(byte[] buf, int idx, float value)
            {
                byte[] b = BitConverter.GetBytes(value);
                if (!BitConverter.IsLittleEndian) Array.Reverse(b);
                Array.Copy(b, 0, buf, idx, 4);
            }
        }

        private class VmxSpiAlgaritm : VmxSpi
        {
            protected override void ParseRx(byte[] data)
            {
                if (data.Length == 0) return;
                if (data[0] == 1)
                {
                    _ri.Analog1 = (data[2] << 8) | data[1];
                    _ri.Analog2 = (data[4] << 8) | data[3];
                    _ri.Analog3 = (data[6] << 8) | data[5];
                    _ri.Analog4 = (data[8] << 8) | data[7];
                    _ri.Analog5 = (data[10] << 8) | data[9];
                    _ri.Analog6 = (data[12] << 8) | data[11];
                    _ri.Analog7 = (data[14] << 8) | data[13];
                }
                else if (data[0] == 2)
                {
                    _ri.Analog8 = (data[2] << 8) | data[1];
                    _ri.Ultrasound1 = ((data[4] << 8) | data[3]) / 100.0f;
                    _ri.Ultrasound2 = ((data[6] << 8) | data[5]) / 100.0f;
                    _ri.Ultrasound3 = ((data[8] << 8) | data[7]) / 100.0f;
                    _ri.Ultrasound4 = ((data[10] << 8) | data[9]) / 100.0f;

                    _ri.Inputs[0] = Funcad.AccessBit(data[11], 0);
                    _ri.Inputs[1] = Funcad.AccessBit(data[11], 1);
                    _ri.Inputs[2] = Funcad.AccessBit(data[11], 2);
                    _ri.Inputs[3] = Funcad.AccessBit(data[11], 3);
                }
                else if (data[0] == 3)
                {
                    int yawUi = (data[2] << 8) | data[1];
                    float newYaw = (yawUi / 100.0f) * (Funcad.AccessBit(data[7], 1) ? 1f : -1f);
                    _ri.YawUnlim += CalcAngleUnlim(newYaw, _ri.Yaw);
                    _ri.Yaw = newYaw;

                    int pitchUi = (data[4] << 8) | data[3];
                    float newPitch = (pitchUi / 100.0f) * (Funcad.AccessBit(data[7], 2) ? 1f : -1f);
                    _ri.PitchUnlim += CalcAngleUnlim(newPitch, _ri.Pitch);
                    _ri.Pitch = newPitch;

                    int rollUi = (data[6] << 8) | data[5];
                    float newRoll = (rollUi / 100.0f) * (Funcad.AccessBit(data[7], 3) ? 1f : -1f);
                    _ri.RollUnlim += CalcAngleUnlim(newRoll, _ri.Roll);
                    _ri.Roll = newRoll;

                    _robot.Power = ((data[9] << 8) | data[8]) / 100.0f;
                }
            }

            protected override byte[] BuildTx()
            {
                byte[] tx = new byte[16];
                if (_toggler == 0)
                {
                    tx[0] = 1;
                    for (int i = 0; i < 8; i++) tx[1 + i] = (byte)_ri.ServoAngles[i];

                    // bits: '1' + out0 + out1 + out2 + out3 + '001'
                    byte outByte = 0b10000001;
                    if (_ri.Outputs[0]) outByte |= (1 << 6);
                    if (_ri.Outputs[1]) outByte |= (1 << 5);
                    if (_ri.Outputs[2]) outByte |= (1 << 4);
                    if (_ri.Outputs[3]) outByte |= (1 << 3);
                    tx[9] = outByte;
                }
                return tx;
            }

            private static float CalcAngleUnlim(float newAngle, float oldAngle)
            {
                float delta = newAngle - oldAngle;
                if (delta < -180f) delta = (180f - oldAngle) + (180f + newAngle);
                else if (delta > 180f) delta = -(180f + oldAngle) - (180f - newAngle);
                return delta;
            }
        }
    }
}
