using System;
using System.Threading;
using OpenCvSharp;
using RobocadCs;
using RobocadCs.Internal.Common;


static class Program
{
    const float DEADZONE = 10f;
    const int TRIGGER_THRESHOLD = 120;

    static float StickToSpeed(int stick) => stick / 800f;

    static float TrigToSpeed(byte leftTrigger, byte rightTrigger)
    {
        float sp = 0;
        if (leftTrigger > TRIGGER_THRESHOLD) sp += 40;
        if (rightTrigger > TRIGGER_THRESHOLD) sp -= 40;
        return sp;
    }

    static float ApplyDeadzone(float speed) => Math.Abs(speed) > DEADZONE ? speed : 0f;

    static int Main(string[] args)
    {
        int runSeconds = (args.Length > 0 && int.TryParse(args[0], out int s)) ? s : -1;

        var robot = new RobotAlgaritm(false);
        var dash = new Shufflecad(robot);
        Thread.Sleep(100);

        robot.SetPidSettings(true, 0.14f, 0.1f, 0);

        dash.PrintToLog($"Test log");

        var vYaw = dash.AddVar(new ShuffleVariable("yaw", ShuffleVariable.FloatType, ShuffleVariable.InVar ));
        var vYawG = dash.AddVar(new ShuffleVariable("yaw_g", ShuffleVariable.ChartType, ShuffleVariable.OutVar));
        var vUs1 = dash.AddVar(new ShuffleVariable("us_1", ShuffleVariable.FloatType, ShuffleVariable.OutVar));
        var vUs2 = dash.AddVar(new ShuffleVariable("us_2", ShuffleVariable.FloatType, ShuffleVariable.OutVar));
        var vEnc0 = dash.AddVar(new ShuffleVariable("enc_0", ShuffleVariable.FloatType, ShuffleVariable.OutVar));
        var vEnc1 = dash.AddVar(new ShuffleVariable("enc_1", ShuffleVariable.FloatType, ShuffleVariable.OutVar));
        var vPower = dash.AddVar(new ShuffleVariable("power", ShuffleVariable.FloatType, ShuffleVariable.OutVar));
        var vDrive = dash.AddVar(new ShuffleVariable("drive", ShuffleVariable.StringType, ShuffleVariable.OutVar));
        var vLidar = dash.AddVar(new ShuffleVariable("lidar", ShuffleVariable.RadarType, ShuffleVariable.OutVar));
        var analog1 = dash.AddVar(new ShuffleVariable("analog1", ShuffleVariable.FloatType, ShuffleVariable.OutVar));
        var vOutputs = dash.AddVar(new ShuffleVariable("outputs", ShuffleVariable.StringType, ShuffleVariable.OutVar));
        var vServo = dash.AddVar(new ShuffleVariable("servo_1", ShuffleVariable.FloatType, ShuffleVariable.InVar));
        var vJoyRaw = dash.AddVar(new ShuffleVariable("joy_raw", ShuffleVariable.StringType, ShuffleVariable.OutVar));
        var cameraWidth = dash.AddVar(new ShuffleVariable("cameraWidth", ShuffleVariable.FloatType, ShuffleVariable.InVar));

        var cam = dash.AddVar(new CameraVariable("camera"));

        Console.WriteLine("RobocadDemo запущен (симулятор): робот управляется геймпадом,");
        Console.WriteLine("левый стик — движение, триггеры — поворот, правый стик — 4-й мотор,");
        Console.WriteLine("A — серво 1, B — серво 2.");
        Console.WriteLine("Датчики и камера уходят в Shufflecad (порты 63253–63259).");
        Console.WriteLine(runSeconds > 0 ? $"Автоостановка через {runSeconds} c." : "Ctrl+C — выход.");

        // Omni-kinematics: left stick drives X/Y, triggers add rotation.
        void Drive()
        {
            var joy = dash.JoystickData;

            float x = StickToSpeed(joy.LeftStickX);
            float y = StickToSpeed(joy.LeftStickY);
            float r = TrigToSpeed(joy.LeftTrigger, joy.RightTrigger);

            robot.MotorSpeed3 = ApplyDeadzone(y - x / 2 + r);
            robot.MotorSpeed2 = ApplyDeadzone(-y - x / 2 + r);
            robot.MotorSpeed0 = ApplyDeadzone(x + r);
            robot.MotorSpeed1 = ApplyDeadzone(StickToSpeed(joy.RightStickY) / 4);

            robot.SetAngleServo(joy.BtnA ? 180 : 0, 1);
            robot.SetAngleServo(joy.BtnB ? 180 : 0, 2);
        }

        void Halt()
        {
            robot.MotorSpeed0 = 0;
            robot.MotorSpeed1 = 0;
            robot.MotorSpeed2 = 0;
            robot.MotorSpeed3 = 0;
        }

        long frame = 0;
        var startUtc = DateTime.UtcNow;

        while (runSeconds <= 0 || (DateTime.UtcNow - startUtc).TotalSeconds < runSeconds)
        {
            Drive();
            string drive = $"{robot.MotorSpeed0:0.#}/{robot.MotorSpeed1:0.#}/{robot.MotorSpeed2:0.#}/{robot.MotorSpeed3:0.#}";

            var j = dash.JoystickData;
            string joyRaw = $"LX={j.LeftStickX} LY={j.LeftStickY} RX={j.RightStickX} RY={j.RightStickY} " +
                            $"LT={j.LeftTrigger} RT={j.RightTrigger} A={j.BtnA} B={j.BtnB}";
            vJoyRaw.SetString(joyRaw);
            if (frame % 100 == 0) Console.WriteLine($"[joy] {joyRaw}  ->  motors {drive}");

            vYaw.SetFloat(robot.Yaw);
            vYawG.SetFloat(robot.Yaw);
            vUs1.SetFloat(robot.Us1);
            vUs2.SetFloat(robot.Us2);
            vEnc0.SetFloat(robot.MotorEnc0);
            vEnc1.SetFloat(robot.MotorEnc1);
            vPower.SetFloat(robot.Power);
            vDrive.SetString(drive);
            analog1.SetFloat(robot.Analog1);
            
            float[] lidar = robot.LidarData;
            if (lidar.Length > 0) vLidar.SetRadar(lidar);

            // Port 8: servos 1 and 2 are driven by the gamepad in Drive().
            robot.SetAngleServo(vServo.GetFloat(), 8);
            
            const int OUT_COUNT = 2;
            int activeOut = (int)((frame / 20) % OUT_COUNT);
            for (int i = 0; i < OUT_COUNT; i++)
                robot.Outputs[i] = (i == activeOut);

            using (Mat img = robot.CameraImage)
            {
                if (img != null)
                {
                    int targetW = (int)cameraWidth.GetFloat();
                    if (targetW > 0 && targetW != img.Width && img.Width > 0 && img.Height > 0)
                    {
                        int targetH = Math.Max(1, (int)((long)img.Height * targetW / img.Width));
                        using var resized = new Mat();
                        Cv2.Resize(img, resized, new Size(targetW, targetH));
                        cam.SetMat(resized);
                    }
                    else
                    {
                        cam.SetMat(img);
                    }
                }
            }

            frame++;
            Thread.Sleep(5);
        }

        Halt();
        dash.Stop();
        robot.Stop();
        return 0;
    }
}
