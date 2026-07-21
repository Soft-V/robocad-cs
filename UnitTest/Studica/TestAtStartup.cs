namespace UnitTest.Studica
{
    [Collection("RobocadStudicaHandlerCollection")]
    public class TestAtStartup
    {
        private readonly RobotHandlerFixture _handler;
        public TestAtStartup(RobotHandlerFixture handler)
        {
            _handler = handler;
        }

        [Fact]
        public void TestAnalogOutputOnRobotStartup()
        {
            Assert.NotEqual(0.0f, _handler.robot.Analog1);
            Assert.NotEqual(0.0f, _handler.robot.Analog2);
            Assert.NotEqual(0.0f, _handler.robot.Analog3);
            Assert.NotEqual(0.0f, _handler.robot.Analog4);
        }

        [Fact]
        public void TestUltrasonicOutputOnRobotStartup()
        {
            Assert.NotEqual(0.0f, _handler.robot.Us1);
            Assert.NotEqual(0.0f, _handler.robot.Us2);
        }

        [Fact]
        public void TestYawOutputOnRobotStartup()
        {
            Assert.NotEqual(0.0f, _handler.robot.Yaw);
        }

        [Fact]
        public void TestIfYawResetWorks()
        {
            //_handler.robot.ResetYaw(); // я пока не знаю как будет реализован сброс и возможно ли получить такую точность
            //Assert.Equal(0.0f, _handler.robot.Yaw, 0.000_000_1); // Там 15 знаков после запятой у Yaw, поэтому пусть погрешность будет 1 миллионная градуса. Потом может поменяю
        }


        [Fact]
        public void TestIfEncodersAreZeroOnStartup()
        {
            Assert.Equal(0.0f, _handler.robot.MotorEnc0);
            Assert.Equal(0.0f, _handler.robot.MotorEnc1);
            Assert.Equal(0.0f, _handler.robot.MotorEnc2);
            Assert.Equal(0.0f, _handler.robot.MotorEnc3);
        }

        [Fact]
        public void TestIfAllButtonsAreFalseOnRobotStartup()
        { 
            Assert.False(_handler.robot.VmxFlex[0]);
            Assert.False(_handler.robot.VmxFlex[1]);
            Assert.False(_handler.robot.VmxFlex[2]);
            Assert.False(_handler.robot.VmxFlex[3]);
        }
    }
}
