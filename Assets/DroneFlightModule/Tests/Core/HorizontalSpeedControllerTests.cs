using System;
using NUnit.Framework;

namespace DroneFlightModule.Core.Tests
{
    public class HorizontalSpeedControllerTests
    {
        private const float Gain = 2f;
        private const float Tolerance = 1e-4f;

        private readonly DroneConfig drone = DroneConfig.Default;

        private HorizontalSpeedController controller;

        [SetUp]
        public void SetUp()
        {
            controller = new HorizontalSpeedController(drone, Gain);
        }

        [Test]
        public void Constructor_RejectsInvalidArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new HorizontalSpeedController(null, Gain));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HorizontalSpeedController(drone, 0f));
        }

        [Test]
        public void AtTargetVelocity_TheForceOnlyCancelsDrag()
        {
            var velocity = new Vec3(1f, 0f, -0.5f);

            var force = controller.Force(velocity, velocity);

            Assert.That(force.X, Is.EqualTo(drone.LinearDrag * velocity.X).Within(Tolerance));
            Assert.That(force.Z, Is.EqualTo(drone.LinearDrag * velocity.Z).Within(Tolerance));
        }

        [Test]
        public void ItPushesTowardsTheTarget_AndIgnoresVerticalSpeed()
        {
            var force = controller.Force(new Vec3(0.5f, 9f, 0f), new Vec3(0f, -3f, 0f));

            Assert.That(force.X, Is.GreaterThan(0f));
            Assert.That(force.Y, Is.EqualTo(0f));
            Assert.That(force.Z, Is.EqualTo(0f));
        }

        [Test]
        public void TheForce_IsLimitedByTheFrame()
        {
            var force = controller.Force(Vec3.Zero, new Vec3(30f, 0f, 30f));

            Assert.That(force.Magnitude, Is.EqualTo(drone.MaxLateralForce).Within(Tolerance));
        }
    }
}
