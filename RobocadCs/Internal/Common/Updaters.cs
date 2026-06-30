using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace RobocadCs.Internal.Common
{
    public class Updater
    {
        private readonly Robot Robot;
        public volatile bool StopRobotInfoThread = false;

        public Updater(Robot robot)
        {
            Robot = robot;
        }

        protected static float ReadCpuTemperature()
        {
            try
            {
                string line = File.ReadAllText("/sys/class/thermal/thermal_zone0/temp").Trim();
                if (int.TryParse(line, out int milli)) return milli / 1000.0f;
            }
            catch
            {
            }

            return 0.0f;
        }

        private static (long total, long idle) ReadCpuStats()
        {
            try
            {
                string line = File.ReadLines("/proc/stat").FirstOrDefault();
                if (line == null) return (0, 0);
                string[] p = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                // p[0] == "cpu"
                long[] v = new long[Math.Max(0, p.Length - 1)];
                for (int i = 1; i < p.Length; i++) long.TryParse(p[i], out v[i - 1]);
                if (v.Length < 4) return (0, 0);
                long idle = v[3];
                long total = 0;
                for (int i = 0; i < Math.Min(8, v.Length); i++) total += v[i];
                return (total, idle);
            }
            catch
            {
                return (0, 0);
            }
        }

        protected static float GetCpuLoad()
        {
            var s1 = ReadCpuStats();
            Thread.Sleep(500);
            var s2 = ReadCpuStats();
            if (s1.total == 0 || s2.total == 0) return 0.0f;
            long totalDiff = s2.total - s1.total;
            long idleDiff = s2.idle - s1.idle;
            if (totalDiff <= 0) return 0.0f;
            return (totalDiff - idleDiff) * 100.0f / totalDiff;
        }

        protected static float GetMemoryLoad()
        {
            try
            {
                long total = 0, available = 0;
                foreach (var raw in File.ReadLines("/proc/meminfo"))
                {
                    if (raw.StartsWith("MemTotal:")) total = ParseKb(raw);
                    else if (raw.StartsWith("MemAvailable:")) available = ParseKb(raw);
                }

                if (total == 0) return 0.0f;
                long used = total - available;
                return used * 100.0f / total;
            }
            catch
            {
                return 0.0f;
            }
        }

        private static long ParseKb(string line)
        {
            string[] p = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return p.Length >= 2 && long.TryParse(p[1], out long v) ? v : 0;
        }

        public void RunUpdater() => Loop();

        private void Loop()
        {
            while (!StopRobotInfoThread)
            {
                try
                {
                    float cpu = GetCpuLoad();
                    Robot.RobotInfo.CpuLoad = cpu;
                    Robot.RobotInfo.Temperature = ReadCpuTemperature();
                    Robot.RobotInfo.MemoryLoad = GetMemoryLoad();
                    Thread.Sleep(500);
                }
                catch (Exception e)
                {
                    Robot.WriteLog("Info thread error: " + e.Message);
                }
            }
        }
    }
}