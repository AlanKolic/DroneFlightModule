using System;
using NUnit.Framework;

namespace DroneFlightModule.Core.Tests
{
    public class RangefinderAltitudeSensorTests
    {
        private const float DeltaTime = TestConfigs.DeltaTime;

        [Test]
        public void MeasuresHeightAboveTerrain()
        {
            var sensor = new RangefinderAltitudeSensor(new FlatTerrain(height: 5f), RangefinderConfig.Default);

            var reading = sensor.Read(new DroneState(new Vec3(0f, 8f, 0f)), 0.0);

            Assert.That(reading.IsValid, Is.True);
            Assert.That(reading.HeightAboveGround, Is.EqualTo(3f));
        }

        [Test]
        public void MeasuresTerrainDirectlyBelowDrone()
        {
            var sensor = new RangefinderAltitudeSensor(new SlopedTerrain(), RangefinderConfig.Default);

            var reading = sensor.Read(new DroneState(new Vec3(4f, 6f, 0f)), 0.0);

            Assert.That(reading.HeightAboveGround, Is.EqualTo(2f));
        }

        [Test]
        public void OnTheGround_ReadsZero()
        {
            var sensor = new RangefinderAltitudeSensor(new FlatTerrain(), RangefinderConfig.Default);

            var reading = sensor.Read(new DroneState(Vec3.Zero), 0.0);

            Assert.That(reading.IsValid, Is.True);
            Assert.That(reading.HeightAboveGround, Is.EqualTo(0f));
        }

        [Test]
        public void AtMaxRange_IsValid()
        {
            var sensor = new RangefinderAltitudeSensor(new FlatTerrain(), new RangefinderConfig(maxRange: 10f, updateRate: 20f));

            var reading = sensor.Read(new DroneState(new Vec3(0f, 10f, 0f)), 0.0);

            Assert.That(reading.IsValid, Is.True);
            Assert.That(reading.HeightAboveGround, Is.EqualTo(10f));
        }

        [Test]
        public void AboveMaxRange_IsInvalid()
        {
            var sensor = new RangefinderAltitudeSensor(new FlatTerrain(), new RangefinderConfig(maxRange: 10f, updateRate: 20f));

            var reading = sensor.Read(new DroneState(new Vec3(0f, 10.5f, 0f)), 1.5);

            Assert.That(reading.IsValid, Is.False);
        }

        [Test]
        public void BetweenSamples_HoldsLastReading()
        {
            var sensor = new RangefinderAltitudeSensor(new FlatTerrain(), new RangefinderConfig(maxRange: 10f, updateRate: 10f));
            sensor.Read(new DroneState(new Vec3(0f, 2f, 0f)), 0.0);

            var held = sensor.Read(new DroneState(new Vec3(0f, 4f, 0f)), 0.05);

            Assert.That(held.HeightAboveGround, Is.EqualTo(2f));
        }

        [Test]
        public void AfterSamplePeriod_TakesNewReading()
        {
            var sensor = new RangefinderAltitudeSensor(new FlatTerrain(), new RangefinderConfig(maxRange: 10f, updateRate: 10f));
            sensor.Read(new DroneState(new Vec3(0f, 2f, 0f)), 0.0);

            var fresh = sensor.Read(new DroneState(new Vec3(0f, 4f, 0f)), 0.1);

            Assert.That(fresh.HeightAboveGround, Is.EqualTo(4f));
        }

        [Test]
        public void FixedTicks_SampleOncePerPeriod()
        {
            var sensor = new RangefinderAltitudeSensor(new FlatTerrain(), new RangefinderConfig(maxRange: 10f, updateRate: 10f));
            int samples = 0;
            float lastHeight = -1f;
            double time = 0.0;

            // The height changes every tick, so a changed reading means a new sample.
            for (int i = 0; i < 50; i++)
            {
                var reading = sensor.Read(new DroneState(new Vec3(0f, i * 0.1f, 0f)), time);
                if (reading.HeightAboveGround != lastHeight)
                {
                    samples++;
                    lastHeight = reading.HeightAboveGround;
                }
                time += DeltaTime;
            }

            Assert.That(samples, Is.EqualTo(10));
        }

        [TestCase(0f, 20f)]
        [TestCase(-1f, 20f)]
        [TestCase(float.PositiveInfinity, 20f)]
        [TestCase(10f, 0f)]
        [TestCase(10f, float.NaN)]
        public void Config_RejectsInvalidSettings(float maxRange, float updateRate)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RangefinderConfig(maxRange, updateRate));
        }

        [Test]
        public void Constructor_RequiresTerrainAndConfig()
        {
            Assert.Throws<ArgumentNullException>(() => new RangefinderAltitudeSensor(null, RangefinderConfig.Default));
            Assert.Throws<ArgumentNullException>(() => new RangefinderAltitudeSensor(new FlatTerrain(), null));
        }
    }
}
