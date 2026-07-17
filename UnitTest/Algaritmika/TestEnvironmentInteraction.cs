using Xunit.v3.Priority;

namespace UnitTest.Algaritmika
{
    [Collection("RobocadAlgaritmikaHandlerCollection"), TestCaseOrderer(typeof(PriorityOrderer))]
    public class TestEnvironmentInteraction
    {
        private readonly RobotHandlerFixture _handler;
        public TestEnvironmentInteraction(RobotHandlerFixture handler) {
            _handler = handler;
        }

        [Fact, Priority(1)]
        public async Task TestAxisSpeedDistribution()
        {
            await Task.Delay(500);
            SetAxisSpeed(50, 0, 0); // Ровно прямо
            await Task.Delay(2000);
            SetAxisSpeed(0, 50, 0); // Ровно вправо без скосов
            await Task.Delay(2000);
            SetAxisSpeed(0, 0, 50); // По часовой
            await Task.Delay(2000);
            SetAxisSpeed(0, 0, 0);
        }

        [Fact, Priority(2)]
        public void TestBaseEncodersValuesAfterRobotMoved()
        {
            Assert.NotEqual(0.0f, _handler.robot.MotorEnc0);
            Assert.NotEqual(0.0f, _handler.robot.MotorEnc1);
            Assert.NotEqual(0.0f, _handler.robot.MotorEnc2);
        }

        [Fact, Priority(3)]
        public void TestIfBaseEncodersResetWorks()
        {
            _handler.robot.ResetMotorEnc0();
            _handler.robot.ResetMotorEnc1();
            _handler.robot.ResetMotorEnc2();
            Assert.Equal(0.0f, _handler.robot.MotorEnc0);
            Assert.Equal(0.0f, _handler.robot.MotorEnc1);
            Assert.Equal(0.0f, _handler.robot.MotorEnc2);
                
        }

        [Fact(Timeout = 30_000)]
        public void TestButtonsReactionOnUserInteraction()
        {
            bool emsButtonPressedOnce = false;
            bool startButtonPressedOnce = false;
            bool resetButtonPressedOnce = false;
            bool stopButtonPressedOnce = false;

            while(!emsButtonPressedOnce || !startButtonPressedOnce || !resetButtonPressedOnce || !stopButtonPressedOnce)
            {
                if (_handler.robot.Inputs[0])
                    emsButtonPressedOnce = true;
                if (_handler.robot.Inputs[1])
                    startButtonPressedOnce = true;
                if (_handler.robot.Inputs[2])
                    resetButtonPressedOnce = true;
                if (_handler.robot.Inputs[3])
                    stopButtonPressedOnce = true;
            }

            Assert.True(emsButtonPressedOnce);
            Assert.True(startButtonPressedOnce);
            Assert.True(resetButtonPressedOnce);
            Assert.True(stopButtonPressedOnce);  
        }

        [Fact]
        public async Task TestElevatorServoInteraction()
        {
            await Task.Delay(500);
            _handler.robot.SetAngleServo(180, 1);
            await Task.Delay(2000);
            _handler.robot.SetAngleServo(90, 1);
            await Task.Delay(1500);
            _handler.robot.SetAngleServo(0, 1);
            await Task.Delay(1500);
        }

        [Fact]
        public async Task TestGripperServoInteraction()
        {
            await Task.Delay(500);
            _handler.robot.SetAngleServo(180, 2);
            await Task.Delay(1500);
            _handler.robot.SetAngleServo(90, 2);
            await Task.Delay(1500);
            _handler.robot.SetAngleServo(0, 2);
            await Task.Delay(1500);
        }

        [Fact]
        public async Task TestIfLEDsAreWorkingAndAttachedCorrectly()
        {
            await Task.Delay(500);
            _handler.robot.Outputs[0] = true; // Зеленый
            _handler.robot.Outputs[1] = false; 
            await Task.Delay(1000);
            _handler.robot.Outputs[1] = true; // Красный
            _handler.robot.Outputs[0] = false;
            await Task.Delay(1000);
        }

        private void SetAxisSpeed(float x, float y, float z)
        {
            float right = -x + y / 2 + z;
            float left = x + y / 2 + z;
            float back = -y + z;

            _handler.robot.MotorSpeed0 = left;
            _handler.robot.MotorSpeed1 = right;
            _handler.robot.MotorSpeed2 = back;
        }
    }
}
