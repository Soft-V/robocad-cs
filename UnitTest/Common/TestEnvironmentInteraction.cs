using Xunit.v3.Priority;

namespace UnitTest.Common
{
    [Collection("RobocadCommonHandlerCollection"), TestCaseOrderer(typeof(PriorityOrderer))]
    public class TestEnvironmentInteraction
    {
        private readonly RobotHandlerFixture _handler;
        public TestEnvironmentInteraction(RobotHandlerFixture handler)
        {
            _handler = handler;
        }

        [Fact, Priority(1)]
        public async Task TestAxisSpeedDistribution()
        {
            await Task.Delay(500);
            SetAxisSpeed(50, 0); // Прямо, но будет скос вправо
            await Task.Delay(7000);
            SetAxisSpeed(0, 50); // По часовой
            await Task.Delay(2000);
            SetAxisSpeed(0, 0);
        }

        [Fact, Priority(2)]
        public void TestBaseEncodersValuesAfterRobotMoved()
        {
            Assert.NotEqual(0.0f, _handler.robot.MotorEnc0);
            Assert.NotEqual(0.0f, _handler.robot.MotorEnc1);
            Assert.NotEqual(0.0f, _handler.robot.MotorEnc2);
            Assert.NotEqual(0.0f, _handler.robot.MotorEnc3);
            Assert.NotEqual(0.0f, _handler.robot.MotorEnc4);
            Assert.NotEqual(0.0f, _handler.robot.MotorEnc5);
        }

        [Fact, Priority(3)]
        public void TestIfBaseEncodersResetWorks()
        {
            _handler.robot.ResetMotorEnc0();
            _handler.robot.ResetMotorEnc1();
            _handler.robot.ResetMotorEnc2();
            _handler.robot.ResetMotorEnc3();
            _handler.robot.ResetMotorEnc4();
            _handler.robot.ResetMotorEnc5();
            Assert.Equal(0.0f, _handler.robot.MotorEnc0);
            Assert.Equal(0.0f, _handler.robot.MotorEnc1);
            Assert.Equal(0.0f, _handler.robot.MotorEnc2);
            Assert.Equal(0.0f, _handler.robot.MotorEnc3);
            Assert.Equal(0.0f, _handler.robot.MotorEnc4);
            Assert.Equal(0.0f, _handler.robot.MotorEnc5);

        }

        [Fact, Priority(4)]
        public async Task TestElevatingArmServoInteraction()
        {
            await Task.Delay(500);
            _handler.robot.SetAngleServo(300, 1);
            await Task.Delay(4000);
            _handler.robot.SetAngleServo(150, 1);
            await Task.Delay(3000);
            _handler.robot.SetAngleServo(0, 1);
            await Task.Delay(3000);
        }

        [Fact]
        public async Task TestGripperServoInteraction()
        {
            await Task.Delay(500);
            _handler.robot.SetAngleServo(300, 2);
            await Task.Delay(2500);
            _handler.robot.SetAngleServo(150, 2);
            await Task.Delay(2500);
            _handler.robot.SetAngleServo(0, 2);
            await Task.Delay(2500);
        }

        [Fact(Timeout = 30_000)]
        public void TestButtonsReactionOnUserInteraction()
        {
            bool emsButtonPressedOnce = false;
            bool startButtonPressedOnce = false;
            bool resetButtonPressedOnce = false;
            bool stopButtonPressedOnce = false;

            while (!emsButtonPressedOnce || !startButtonPressedOnce || !resetButtonPressedOnce || !stopButtonPressedOnce)
            {
                if (_handler.robot.Buttons[0])
                    emsButtonPressedOnce = true;
                if (_handler.robot.Buttons[1])
                    startButtonPressedOnce = true;
                if (_handler.robot.Buttons[2])
                    resetButtonPressedOnce = true;
                if (_handler.robot.Buttons[3])
                    stopButtonPressedOnce = true;
            }

            Assert.True(emsButtonPressedOnce);
            Assert.True(startButtonPressedOnce);
            Assert.True(resetButtonPressedOnce);
            Assert.True(stopButtonPressedOnce);
        }

        private void SetAxisSpeed(float x, float z)
        {
            float right1 = -x + z;
            float right2 = -x + z;
            float right3 = -x + z;

            float left1 = x + z;
            float left2 = x + z;
            float left3 = x + z;

            _handler.robot.MotorSpeed0 = left1;
            _handler.robot.MotorSpeed1 = left2;
            _handler.robot.MotorSpeed2 = left3;

            _handler.robot.MotorSpeed3 = right1;
            _handler.robot.MotorSpeed4 = right2;
            _handler.robot.MotorSpeed5 = right3;
        }
    }
}