using System;
using NUnit.Framework;

namespace DroneFlightModule.Core.Tests
{
    public class FailsafeConfigTests
    {
        private FailsafeConfig Create(
            float holdDuration = 10f,
            float descentSinkRate = 1f,
            float touchdownSinkRate = 0.3f,
            float flareHeight = 2f,
            float fallbackSinkRate = 0.5f,
            float verticalSpeedGain = 4f)
        {
            return new FailsafeConfig(
                holdDuration,
                descentSinkRate,
                touchdownSinkRate,
                flareHeight,
                fallbackSinkRate,
                verticalSpeedGain);
        }

        [Test]
        public void Default_HoldsForTenSeconds()
        {
            Assert.That(FailsafeConfig.Default.HoldDuration, Is.EqualTo(10f));
        }

        [Test]
        public void ZeroHoldDuration_IsAllowed()
        {
            Assert.That(Create(holdDuration: 0f).HoldDuration, Is.EqualTo(0f));
        }

        [Test]
        public void RejectsInvalidValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(holdDuration: -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(descentSinkRate: 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(touchdownSinkRate: float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(flareHeight: float.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(fallbackSinkRate: -0.5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(verticalSpeedGain: 0f));
        }

        [Test]
        public void RejectsTouchdownFasterThanDescent()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(descentSinkRate: 0.5f, touchdownSinkRate: 0.8f));
        }
    }
}
