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
            Assert.That(config.DroneHistoryLength, Is.GreaterThan(0));
            Assert.That(config.FleetHistoryLength, Is.GreaterThan(0));
        }

        [Test]
        public void RejectsValuesThatAreNotPositive()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TelemetryConfig(0f, 10, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TelemetryConfig(float.NaN, 10, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TelemetryConfig(10f, 0, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TelemetryConfig(10f, 10, 0));
        }
    }
}
