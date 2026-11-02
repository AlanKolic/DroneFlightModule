using System;
using NUnit.Framework;

namespace DroneFlightModule.Core.Tests
{
    public class VelocityControllerConfigTests
    {
        private VelocityControllerConfig Create(
            float maxHorizontalSpeed = 8f,
            float maxClimbRate = 3f,
            float maxYawRate = 1.5f,
            float horizontalSpeedGain = 2f,
            float verticalSpeedGain = 4f,
            float yawRateGain = 8f)
        {
            return new VelocityControllerConfig(
                maxHorizontalSpeed,
                maxClimbRate,
                maxYawRate,
                horizontalSpeedGain,
                verticalSpeedGain,
                yawRateGain);
        }

        [Test]
        public void Default_IsValid()
        {
            Assert.DoesNotThrow(() => new VelocityFlightController(DroneConfig.Default, VelocityControllerConfig.Default));
        }

        [Test]
        public void RejectsNonPositiveOrNonFiniteValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(maxHorizontalSpeed: 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(maxClimbRate: -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(maxYawRate: float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(horizontalSpeedGain: float.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(verticalSpeedGain: 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(yawRateGain: -2f));
        }
    }
}
