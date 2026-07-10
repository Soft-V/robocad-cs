using System;
using System.Threading;
using OpenCvSharp;
using RobocadCs;
using RobocadCs.Internal.Common;


static class Program
{
    const float SPEED = 30f;
    const int LEG_TICKS = 60;

    static int Main(string[] args)
    {
        int runSeconds = (args.Length > 0 && int.TryParse(args[0], out int s)) ? s : -1;

        var robot = new RobotAlgaritm(false);
        var dash = new Shufflecad(robot);
        Thread.Sleep(100);

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
        var vServo = dash.AddVar(new ShuffleVariable("servo_1", ShuffleVariable.FloatType, ShuffleVariable.InVar));
        var cameraWidth = dash.AddVar(new ShuffleVariable("cameraWidth", ShuffleVariable.FloatType, ShuffleVariable.InVar));

        var cam = dash.AddVar(new CameraVariable("camera"));

        Console.WriteLine("RobocadDemo запущен (симулятор): робот ездит вперёд-назад,");
        Console.WriteLine("датчики и камера уходят в Shufflecad (порты 63253–63259).");
        Console.WriteLine(runSeconds > 0 ? $"Автоостановка через {runSeconds} c." : "Ctrl+C — выход.");

        void Forward()
        {
            robot.MotorSpeed0 = SPEED;
            robot.MotorSpeed1 = -SPEED;
            robot.MotorSpeed2 = 0;
        }

        void Backward()
        {
            robot.MotorSpeed0 = -SPEED;
            robot.MotorSpeed1 = SPEED;
            robot.MotorSpeed2 = 0;
        }

        void Halt()
        {
            robot.MotorSpeed0 = 0;
            robot.MotorSpeed1 = 0;
            robot.MotorSpeed2 = 0;
        }

        long frame = 0;
        var startUtc = DateTime.UtcNow;

        while (runSeconds <= 0 || (DateTime.UtcNow - startUtc).TotalSeconds < runSeconds)
        {
            bool goingForward = (frame / LEG_TICKS) % 2 == 0;
            if (goingForward) Forward();
            else Backward();
            string drive = goingForward ? "FORWARD" : "BACKWARD";

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

            robot.SetAngleServo(vServo.GetFloat(), 1);

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

            Thread.Sleep(50);
        }

        Halt();
        dash.Stop();
        robot.Stop();
        return 0;
    }
}
