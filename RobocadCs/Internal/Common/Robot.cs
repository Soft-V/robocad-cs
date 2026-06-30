using System;
using System.IO;

namespace RobocadCs.Internal.Common
{
    public class RobotInfo
    {
        public volatile float SpiTimeDev = 0;
        public volatile float RxSpiTimeDev = 0;
        public volatile float TxSpiTimeDev = 0;
        public volatile float SpiCountDev = 0;
        public volatile float ComTimeDev = 0;
        public volatile float RxComTimeDev = 0;
        public volatile float TxComTimeDev = 0;
        public volatile float ComCountDev = 0;
        public volatile float Temperature = 0;
        public volatile float MemoryLoad = 0;
        public volatile float CpuLoad = 0;
    }

    public abstract class Robot : IDisposable
    {
        public bool OnRealRobot { get; }
        public volatile float Power = 0.0f;
        public RobotInfo RobotInfo { get; }

        private readonly object _logLock = new object();
        private readonly StreamWriter _logFile;

        protected Robot(bool onRealRobot, RobotConfiguration conf)
        {
            OnRealRobot = onRealRobot;
            RobotInfo = new RobotInfo();

            string logPath = onRealRobot ? conf.RealLogPath : conf.SimLogPath;
            try
            {
                _logFile = new StreamWriter(new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    AutoFlush = true
                };
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Failed to open log file: " + logPath + " (" + e.Message + ")");
            }
        }

        public void WriteLog(string text)
        {
            lock (_logLock)
            {
                if (_logFile == null) return;
                _logFile.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " " + text);
            }
        }

        public virtual void Dispose()
        {
            lock (_logLock)
            {
                _logFile?.Dispose();
            }

            GC.SuppressFinalize(this);
        }
    }
}
