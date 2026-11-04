using System;
using System.Linq;
using DroneFlightModule.Core;
using NUnit.Framework;

namespace DroneFlightModule.Telemetry.Tests
{
    public class InMemoryTelemetrySinkTests
    {
        private static readonly DroneId Drone1 = new DroneId("drone-1");
        private static readonly DroneId Drone2 = new DroneId("drone-2");

        private InMemoryTelemetrySink sink;

        [SetUp]
        public void SetUp()
        {
            sink = new InMemoryTelemetrySink(new TelemetryConfig(sampleRate: 10f, droneHistoryLength: 10, fleetHistoryLength: 3));
        }

        private FlightFrame Sample(DroneId drone, double time)
        {
            return new FlightFrame(drone, time, ControlInput.Zero, ForceCommand.None, DroneState.At(Vec3.Zero),
                AltitudeReading.Invalid(time), FlightMode.Grounded, 0.0);
        }

        [Test]
        public void Constructor_RequiresAConfig()
        {
            Assert.Throws<ArgumentNullException>(() => new InMemoryTelemetrySink(null));
        }

        [Test]
        public void KeepsTheLatestSampleOfEachDrone()
        {
            sink.Write(Sample(Drone1, 1.0));
            sink.Write(Sample(Drone2, 1.5));
            sink.Write(Sample(Drone1, 2.0));

            Assert.That(sink.TryGetLatest(Drone1, out var latest), Is.True);
            Assert.That(latest.Time, Is.EqualTo(2.0));
            Assert.That(sink.Drones, Is.EquivalentTo(new[] { Drone1, Drone2 }));
        }

        [Test]
        public void KeepsTheFleetsMostRecentSamples_OldestFirst()
        {
            for (int i = 1; i <= 5; i++)
            {
                sink.Write(Sample(i % 2 == 0 ? Drone2 : Drone1, i));
            }

            Assert.That(sink.History.Select(sample => sample.Time), Is.EqualTo(new[] { 3.0, 4.0, 5.0 }));
        }

        [Test]
        public void AnEndedStream_DropsOutOfTheLatest_ButStaysInTheHistory()
        {
            sink.Write(Sample(Drone1, 1.0));

            sink.End(Drone1);

            Assert.That(sink.TryGetLatest(Drone1, out _), Is.False);
            Assert.That(sink.Drones, Is.Empty);
            Assert.That(sink.History, Has.Count.EqualTo(1));
        }
    }
}
