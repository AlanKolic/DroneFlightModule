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
            sink = new InMemoryTelemetrySink();
        }

        private FlightFrame Sample(DroneId drone, double time)
        {
            return new FlightFrame(drone, time, ControlInput.Zero, ForceCommand.None, new DroneState(Vec3.Zero),
                AltitudeReading.Invalid, FlightMode.Grounded, 0.0);
        }

        [Test]
        public void KeepsTheLatestSampleOfEachDrone()
        {
            sink.Write(Sample(Drone1, 1.0));
            sink.Write(Sample(Drone2, 1.5));
            sink.Write(Sample(Drone1, 2.0));

            Assert.That(sink.TryGetLatest(Drone1, out var latest), Is.True);
            Assert.That(latest.Time, Is.EqualTo(2.0));
            Assert.That(sink.TryGetLatest(Drone2, out var other), Is.True);
            Assert.That(other.Time, Is.EqualTo(1.5));
        }

        [Test]
        public void AnEndedStream_DropsOut()
        {
            sink.Write(Sample(Drone1, 1.0));

            sink.End(Drone1);

            Assert.That(sink.TryGetLatest(Drone1, out _), Is.False);
        }
    }
}
