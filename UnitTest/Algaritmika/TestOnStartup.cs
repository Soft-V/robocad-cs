using RobocadCs;
using Xunit;
using Xunit.v3.Priority;
namespace UnitTest.Algaritmika
{
    [Collection("RobocadHandlerCollection")]
    public class TestOnStartup
    {
        private readonly RobotHandlerFixture _handler;
        public TestOnStartup(RobotHandlerFixture handler)
        {
            _handler = handler;
        }

        [Fact]
        public void TestAnalogOutputOnRobotStartup()
        {
            Assert.Multiple(
                () => Assert.NotEqual(0.0f, _handler.robot.Analog1),
                () => Assert.NotEqual(0.0f, _handler.robot.Analog2),
                () => Assert.NotEqual(0.0f, _handler.robot.Analog3),
                () => Assert.NotEqual(0.0f, _handler.robot.Analog4),
                () => Assert.NotEqual(0.0f, _handler.robot.Analog5),
                () => Assert.NotEqual(0.0f, _handler.robot.Analog6),

                () => Assert.NotEqual(0.0f, _handler.robot.Analog7),
                () => Assert.NotEqual(0.0f, _handler.robot.Analog8)
            );
        }

        [Fact]
        public void TestUltrasonicOutputOnRobotStartup()
        {
            Assert.Multiple(
                () => Assert.NotEqual(0.0f, _handler.robot.Us1),
                () => Assert.NotEqual(0.0f, _handler.robot.Us2),

                () => Assert.NotEqual(0.0f, _handler.robot.Us3),
                () => Assert.NotEqual(0.0f, _handler.robot.Us4)
            );
        }

        [Fact]
        public void TestYawOutputOnRobotStartup()
        {
            Assert.NotEqual(0.0f, _handler.robot.Yaw);
        }

        [Fact]
        public void TestRollOutputOnRobotStartup()
        {
            Assert.NotEqual(0.0f, _handler.robot.Roll);
        }

        [Fact]
        public void TestPitchOutputOnRobotStartup()
        {
            Assert.NotEqual(0.0f, _handler.robot.Pitch);
        }

        [Fact]
        public void TestIfYawResetWorks()
        {
            //_handler.robot.ResetYaw(); // я пока не знаю как будет реализован сброс и возможно ли получить такую точность
            //Assert.Equal(0.0f, _handler.robot.Yaw, 0.000_000_1); // Там 15 знаков после запятой у Yaw, поэтому пусть погрешность будет 1 миллионная градуса. Потом может поменяю
        }

        [Fact]
        public void TestIfRollResetWorks()
        {
            //_handler.robot.ResetRoll(); // я пока не знаю как будет реализован сброс и возможно ли получить такую точность
            //Assert.Equal(0.0f, _handler.robot.Roll, 0.000_000_000_1); // Там 22 знаков после запятой у Roll, поэтому пусть погрешность будет 1 миллиардная градуса. Потом может поменяю
        }

        [Fact]
        public void TestIfPitchResetWorks()
        {
            //_handler.robot.ResetPitch(); // я пока не знаю как будет реализован сброс и возможно ли получить такую точность
            //Assert.Equal(0.0f, _handler.robot.Pitch, 0.000_000_1); // Там 15 знаков после запятой у Pitch, поэтому пусть погрешность будет 1 миллиардная градуса. Потом может поменяю
        }

        [Fact]
        public void TestIfEncodersAreZeroOnStartup()
        {
            Assert.Multiple(
                () => Assert.Equal(0.0f, _handler.robot.MotorEnc0),
                () => Assert.Equal(0.0f, _handler.robot.MotorEnc1),
                () => Assert.Equal(0.0f, _handler.robot.MotorEnc2),
                () => Assert.Equal(0.0f, _handler.robot.MotorEnc3)
            );
        }

        [Fact]
        public void TestIfAllButtonsAreFalseOnRobotStartup()
        {
            Assert.Multiple(
                () => Assert.False(_handler.robot.Inputs[0]),
                () => Assert.False(_handler.robot.Inputs[1]),
                () => Assert.False(_handler.robot.Inputs[2]),
                () => Assert.False(_handler.robot.Inputs[3])
            );
        }
    }
}
