using System;
using NUnit.Framework;

namespace DroneFlightModule.Core.Tests
{
    public class ForceFlightControllerTests
    {
        private const float Tolerance = 1e-4f;

        private readonly DroneConfig _config = DroneConfig.Default;
        private ForceFlightController _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new ForceFlightController(_config);
        }

        [Test]
        public void ZeroInput_ProducesNoForceOrTorque()
        {
            var command = _controller.Compute(ControlInput.Zero, DroneState.At(Vec3.Zero));

            Assert.That(command.Force, Is.EqualTo(Vec3.Zero));
            Assert.That(command.YawTorque, Is.EqualTo(0f));
        }

        [Test]
        public void FullClimb_ProducesMaxThrustUp()
        {
            var command = _controller.Compute(new ControlInput(Vec3.Up, 0f), DroneState.At(Vec3.Zero));

            Assert.That(command.Force.Y, Is.EqualTo(_config.MaxThrust));
        }

        [Test]
        public void NegativeClimb_CutsThrustInsteadOfPullingDown()
        {
            var command = _controller.Compute(new ControlInput(-Vec3.Up, 0f), DroneState.At(Vec3.Zero));

            Assert.That(command.Force.Y, Is.EqualTo(0f));
        }

        [Test]
        public void Forward_AtZeroYaw_PushesAlongWorldForward()
        {
            var command = _controller.Compute(new ControlInput(Vec3.Forward, 0f), DroneState.At(Vec3.Zero));

            Assert.That(command.Force.Z, Is.EqualTo(_config.MaxLateralForce).Within(Tolerance));
            Assert.That(command.Force.X, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Forward_AfterQuarterTurn_PushesAlongWorldRight()
        {
            var facingRight = DroneState.At(Vec3.Zero, yaw: MathF.PI / 2f);

            var command = _controller.Compute(new ControlInput(Vec3.Forward, 0f), facingRight);

            Assert.That(command.Force.X, Is.EqualTo(_config.MaxLateralForce).Within(Tolerance));
            Assert.That(command.Force.Z, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void YawInput_ScalesMaxYawTorque()
        {
            var command = _controller.Compute(new ControlInput(Vec3.Zero, -0.5f), DroneState.At(Vec3.Zero));

            Assert.That(command.YawTorque, Is.EqualTo(-0.5f * _config.MaxYawTorque).Within(Tolerance));
        }
    }
}
