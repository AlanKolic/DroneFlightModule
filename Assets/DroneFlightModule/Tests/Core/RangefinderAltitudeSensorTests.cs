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
            var sensor = new RangefinderAltitudeSensor(new FlatTerrain(height: 5f));

            var reading = sensor.Read(DroneState.At(new Vec3(0f, 8f, 0f)), 0.0);

            Assert.That(reading.IsValid, Is.True);
            Assert.That(reading.HeightAboveGround, Is.EqualTo(3f));
        }

        [Test]
        public void MeasuresTerrainDirectlyBelowDrone()
        {
            var sensor = new RangefinderAltitudeSensor(new SlopedTerrain());

            var reading = sensor.Read(DroneState.At(new Vec3(4f, 6f, 0f)), 0.0);

            Assert.That(reading.HeightAboveGround, Is.EqualTo(2f));
        }

        [Test]
        public void OnTheGround_ReadsZero()
        {
            var sensor = new RangefinderAltitudeSensor(new FlatTerrain());

            var reading = sensor.Read(DroneState.At(Vec3.Zero), 0.0);

            Assert.That(reading.IsValid, Is.True);
            Assert.That(reading.HeightAboveGround, Is.EqualTo(0f));
        }

        [Test]
        public void AtMaxRange_IsValid()
        {
            var sensor = new RangefinderAltitudeSensor(new FlatTerrain(), maxRange: 10f);

            var reading = sensor.Read(DroneState.At(new Vec3(0f, 10f, 0f)), 0.0);

            Assert.That(reading.IsValid, Is.True);
            Assert.That(reading.HeightAboveGround, Is.EqualTo(10f));
        }

        [Test]
        public void AboveMaxRange_IsInvalid()
        {
            var sensor = new RangefinderAltitudeSensor(new FlatTerrain(), maxRange: 10f);

            var reading = sensor.Read(DroneState.At(new Vec3(0f, 10.5f, 0f)), 1.5);

            Assert.That(reading.IsValid, Is.False);
            Assert.That(reading.Timestamp, Is.EqualTo(1.5));
        }

        [Test]
        public void BetweenSamples_HoldsLastReading()
        {
            var sensor = new RangefinderAltitudeSensor(new FlatTerrain(), updateRate: 10f);
            sensor.Read(DroneState.At(new Vec3(0f, 2f, 0f)), 0.0);

            var held = sensor.Read(DroneState.At(new Vec3(0f, 4f, 0f)), 0.05);

            Assert.That(held.HeightAboveGround, Is.EqualTo(2f));
            Assert.That(held.Timestamp, Is.EqualTo(0.0));
        }

        [Test]
        public void AfterSamplePeriod_TakesNewReading()
        {
            var sensor = new RangefinderAltitudeSensor(new FlatTerrain(), updateRate: 10f);
            sensor.Read(DroneState.At(new Vec3(0f, 2f, 0f)), 0.0);

            var fresh = sensor.Read(DroneState.At(new Vec3(0f, 4f, 0f)), 0.1);

            Assert.That(fresh.HeightAboveGround, Is.EqualTo(4f));
            Assert.That(fresh.Timestamp, Is.EqualTo(0.1));
        }

        [Test]
        public void FixedTicks_SampleOncePerPeriod()
        {
            var sensor = new RangefinderAltitudeSensor(new FlatTerrain(), updateRate: 10f);
            int samples = 0;
            double lastTimestamp = -1.0;
            double time = 0.0;

            for (int i = 0; i < 50; i++)
            {
                var reading = sensor.Read(DroneState.At(new Vec3(0f, 1f, 0f)), time);
                if (reading.Timestamp != lastTimestamp)
                {
                    samples++;
                    lastTimestamp = reading.Timestamp;
                }
                time += DeltaTime;
            }

            Assert.That(samples, Is.EqualTo(10));
        }

        [Test]
        public void AStepThatDoesNotDivideThePeriod_StillAveragesTheRate()
        {
            var sensor = new RangefinderAltitudeSensor(new FlatTerrain(), updateRate: 10f);
            int samples = 0;
            double lastTimestamp = -1.0;

            for (int i = 0; i < 100; i++)
            {
                var reading = sensor.Read(DroneState.At(new Vec3(0f, 1f, 0f)), i * 0.03);
                if (reading.Timestamp != lastTimestamp)
                {
                    samples++;
                    lastTimestamp = reading.Timestamp;
                }
            }

            Assert.That(samples, Is.EqualTo(30), "3 s at 10 readings per second.");
        }

        [TestCase(0f, 20f)]
        [TestCase(-1f, 20f)]
        [TestCase(float.PositiveInfinity, 20f)]
        [TestCase(10f, 0f)]
        [TestCase(10f, float.NaN)]
        public void Constructor_RejectsInvalidSettings(float maxRange, float updateRate)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new RangefinderAltitudeSensor(new FlatTerrain(), maxRange, updateRate));
        }
    }
}
