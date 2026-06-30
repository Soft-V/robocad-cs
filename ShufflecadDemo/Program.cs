using System;
using System.Threading;
using RobocadCs.Internal.Common;
using RobocadCs.Shufflecad;

sealed class DemoRobot : Robot
{
    public DemoRobot() : base(false, new DefaultCommonConfiguration { SimLogPath = "shufflecad-demo.log" })
    {
    }
}

static class Program
{
    static int Main(string[] args)
    {
        int runSeconds = (args.Length > 0 && int.TryParse(args[0], out int s)) ? s : -1;

        var robot = new DemoRobot();
        var dash = new Shufflecad(robot);

        var counter = dash.AddVar(new ShuffleVariable("counter", ShuffleVariable.FloatType, ShuffleVariable.OutVar));
        var wave = dash.AddVar(new ShuffleVariable("wave", ShuffleVariable.ChartType, ShuffleVariable.OutVar));
        var blinker = dash.AddVar(new ShuffleVariable("blinker", ShuffleVariable.BoolType, ShuffleVariable.OutVar));
        var status = dash.AddVar(new ShuffleVariable("status", ShuffleVariable.StringType, ShuffleVariable.OutVar));
        var radar = dash.AddVar(new ShuffleVariable("radar", ShuffleVariable.RadarType, ShuffleVariable.OutVar));

        var slider = dash.AddVar(new ShuffleVariable("slider", ShuffleVariable.SliderType, ShuffleVariable.InVar));
        var command = dash.AddVar(new ShuffleVariable("command", ShuffleVariable.StringType, ShuffleVariable.InVar));

        Console.WriteLine("Shufflecad demo запущен. Каналы слушают порты 63253–63259.");
        Console.WriteLine("Подключите приложение Shufflecad и наблюдайте переменные.");
        Console.WriteLine(runSeconds > 0 ? $"Автоостановка через {runSeconds} c." : "Ctrl+C — выход.");

        var rnd = new Random();
        double t = 0;
        long frame = 0;
        var startUtc = DateTime.UtcNow;

        while (runSeconds <= 0 || (DateTime.UtcNow - startUtc).TotalSeconds < runSeconds)
        {
            counter.SetFloat(frame);
            wave.SetFloat((float)Math.Sin(t));
            blinker.SetBool((frame / 10) % 2 == 0);
            status.SetString("работает " + DateTime.Now.ToString("HH:mm:ss"));
            radar.SetRadar(new float[]
            {
                (float)(50 + 40 * Math.Sin(t)),
                (float)(60 + 30 * Math.Cos(t)),
                (float)(70 + 20 * Math.Sin(t * 2)),
                (float)(80 + 10 * Math.Cos(t * 2)),
                (float)(50 + 40 * Math.Sin(t + 1)),
                (float)(60 + 30 * Math.Cos(t + 1)),
                (float)(70 + 20 * Math.Sin(t * 2 + 1)),
                (float)(80 + 10 * Math.Cos(t * 2 + 1)),
            });

            robot.RobotInfo.Temperature = (float)(45 + 5 * Math.Sin(t));
            robot.RobotInfo.CpuLoad = (float)(30 + 20 * Math.Abs(Math.Sin(t / 2)));
            robot.RobotInfo.MemoryLoad = (float)(40 + rnd.NextDouble() * 5);
            robot.Power = (float)(12.0 + 0.3 * Math.Sin(t));

            if (frame % 20 == 0)
            {
                dash.PrintToLog($"tick {frame}, slider={slider.GetFloat():0.0}, command='{command.GetString()}'",
                    Shufflecad.LogInfo);
            }

            t += 0.1;
            frame++;
            Thread.Sleep(50);
        }

        dash.Stop();
        Console.WriteLine("Остановлено.");
        return 0;
    }
}