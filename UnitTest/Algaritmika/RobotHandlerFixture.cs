using RobocadCs;

namespace UnitTest.Algaritmika
{
    public class RobotHandlerFixture
    {
        public RobotAlgaritm robot;
        private Shufflecad shufflecad;
        public RobotHandlerFixture()
        {
            robot = new RobotAlgaritm(false);
            shufflecad = new Shufflecad(robot);
            Thread.Sleep(500);
        }
    }
}