namespace RobocadCs.Internal.Common
{
    public abstract class RobotConfiguration
    {
        public int CameraIndex { get; set; } = 0;
        public string LibHolderFirstPath { get; set; } = "/home/pi";
        public bool WithPiBlaster { get; set; } = true;
        public string LidarPort { get; set; } = "/dev/ttyUSB0";

        public string SimLogPath { get; set; } = "./robocad.log";
        public string RealLogPath { get; set; } = "/var/tmp/robocad.log";

        public string TitanPort { get; set; } = "/dev/ttyACM0";
        public int TitanBaud { get; set; } = 115200;
        public string VmxPort { get; set; } = "/dev/spidev1.2";
        public int VmxCh { get; set; } = 2;
        public int VmxSpeed { get; set; } = 1000000;
        public int VmxMode { get; set; } = 0;
    }

    public class DefaultStudicaConfiguration : RobotConfiguration { }

    public class DefaultAlgaritmConfiguration : RobotConfiguration
    {
        public DefaultAlgaritmConfiguration()
        {
            CameraIndex = 0;
            WithPiBlaster = false;
            VmxPort = "/dev/spidev0.0";
            VmxCh = 0;
        }
    }

    public class DefaultCommonConfiguration : RobotConfiguration { }
}
