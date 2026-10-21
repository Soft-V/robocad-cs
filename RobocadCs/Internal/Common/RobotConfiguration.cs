namespace RobocadCs.Internal.Common
{
    public abstract class RobotConfiguration
    {
        public int CameraIndex { get; set; } = 0;
        public string LibHolderFirstPath { get; set; } = "/home/pi";
        // pigpiod is managed by the OS, with its FIFO interface enabled.
        public string PigpioPipePath { get; set; } = "/dev/pigpio";
        public string PigpioResponsePath { get; set; } = "/dev/pigout";
        public int HcdioPwmFrequency { get; set; } = 100;
        public double PigpioTimeout { get; set; } = 1.0;
        public LidarTypes LidarType { get; set; } = LidarTypes.N10Lidar;
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
            VmxPort = "/dev/spidev0.0";
            VmxCh = 0;
        }
    }

    public class DefaultCommonConfiguration : RobotConfiguration { }
}
