using System;
using NUnit.Framework;

namespace DroneFlightModule.Telemetry.Tests
{
    public class TelemetryConfigTests
    {
        [Test]
        public void Default_IsValid()
        {
            var config = TelemetryConfig.Default;

            Assert.That(config.SampleRate, Is.GreaterThan(0f));
        }

        [Test]
        public void RejectsARateThatIsNotPositiveAndFinite()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TelemetryConfig(0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TelemetryConfig(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TelemetryConfig(float.PositiveInfinity));
        }
    }
}
