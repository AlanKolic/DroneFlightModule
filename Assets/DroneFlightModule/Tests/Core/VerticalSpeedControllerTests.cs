using System;
using NUnit.Framework;

namespace DroneFlightModule.Core.Tests
{
    public class VerticalSpeedControllerTests
    {
        private const float Gravity = DroneIntegrator.StandardGravity;
        private const float Gain = 4f;
        private const float Tolerance = 1e-4f;

        private readonly DroneConfig drone = DroneConfig.Default;

        private VerticalSpeedController controller;

        [SetUp]
        public void SetUp()
        {
            controller = new VerticalSpeedController(drone, Gain, Gravity);
        }

        [Test]
        public void Constructor_RejectsInvalidArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new VerticalSpeedController(null, Gain, Gravity));
            Assert.Throws<ArgumentOutOfRangeException>(() => new VerticalSpeedController(drone, 0f, Gravity));
            Assert.Throws<ArgumentOutOfRangeException>(() => new VerticalSpeedController(drone, Gain, float.NaN));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(2.5f)]
        public void AtTargetSpeed_NetVerticalForceIsZero(float speed)
        {
            float thrust = controller.Thrust(speed, speed);

            Assert.That(thrust - drone.Mass * Gravity - drone.LinearDrag * speed, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void BelowTarget_PushesUp_AboveTarget_EasesOff()
        {
            float hover = drone.Mass * Gravity;

            Assert.That(controller.Thrust(0f, -1f), Is.GreaterThan(hover - drone.LinearDrag));
            Assert.That(controller.Thrust(0f, 1f), Is.LessThan(hover + drone.LinearDrag));
        }

        [Test]
        public void Thrust_IsClampedToActuatorLimits()
        {
            Assert.That(controller.Thrust(0f, -50f), Is.EqualTo(drone.MaxThrust));
            Assert.That(controller.Thrust(0f, 50f), Is.EqualTo(0f));
        }
    }
}
