using RobocadCs;

namespace UnitTest.Studica
{
    public class RobotHandlerFixture
    {
        public RobotVMXTitan robot;
        private Shufflecad shufflecad;
        public RobotHandlerFixture()
        {
            robot = new RobotVMXTitan(false);
            shufflecad = new Shufflecad(robot);
            Thread.Sleep(500);
        }
    }
}
