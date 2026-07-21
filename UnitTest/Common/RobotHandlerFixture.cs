using RobocadCs;
using RobocadCs.Common;

namespace UnitTest.Common
{
    public class RobotHandlerFixture
    {
        public CommonRobot robot;
        private Shufflecad shufflecad;
        public RobotHandlerFixture()
        {
            robot = new CommonRobot(false);
            shufflecad = new Shufflecad(robot);
            Thread.Sleep(500);
        }
    }
}