using System;
using DroneFlightModule.Core;
using NUnit.Framework;

namespace DroneFlightModule.Pilots.Tests
{
    public class PilotStatsRecorderTests
    {
        private static readonly PilotId Alice = new PilotId("alice");
        private static readonly PilotId Bob = new PilotId("bob");
        private static readonly DroneId Drone1 = new DroneId("drone-1");
        private static readonly DroneId Drone2 = new DroneId("drone-2");

        private InMemoryPilotStatsStore store;
        private PilotStatsRecorder recorder;

        [SetUp]
        public void SetUp()
        {
            store = new InMemoryPilotStatsStore();
            recorder = new PilotStatsRecorder(store);
        }

        [Test]
        public void Constructor_RequiresStore()
        {
            Assert.Throws<ArgumentNullException>(() => new PilotStatsRecorder(null));
        }

        [Test]
        public void UnknownPilot_HasNoStats()
        {
            var stats = store.Get(Alice);

            Assert.That(stats.Flights, Is.EqualTo(0));
            Assert.That(stats.FlightTime, Is.EqualTo(0.0));
            Assert.That(stats.LastFlight, Is.Null);
        }

        [Test]
        public void FlightStarted_CountsFlightAndRecordsStartTime()
        {
            recorder.FlightStarted(Alice, Drone1, 12.5);

            var stats = store.Get(Alice);
            Assert.That(stats.Flights, Is.EqualTo(1));
            Assert.That(stats.LastFlight, Is.EqualTo(12.5));
            Assert.That(stats.FlightTime, Is.EqualTo(0.0));
            Assert.That(recorder.IsFlying(Alice, Drone1), Is.True);
        }

        [Test]
        public void FlightEnded_AddsDuration()
        {
            recorder.FlightStarted(Alice, Drone1, 10.0);
            recorder.FlightEnded(Alice, Drone1, 25.0);
            recorder.FlightStarted(Alice, Drone1, 30.0);
            recorder.FlightEnded(Alice, Drone1, 35.0);

            var stats = store.Get(Alice);
            Assert.That(stats.Flights, Is.EqualTo(2));
            Assert.That(stats.FlightTime, Is.EqualTo(20.0));
            Assert.That(stats.LastFlight, Is.EqualTo(30.0));
            Assert.That(recorder.IsFlying(Alice, Drone1), Is.False);
        }

        [Test]
        public void OnePilot_TwoDrones_AreSeparateFlights()
        {
            recorder.FlightStarted(Alice, Drone1, 0.0);
            recorder.FlightStarted(Alice, Drone2, 5.0);
            recorder.FlightEnded(Alice, Drone1, 10.0);
            recorder.FlightEnded(Alice, Drone2, 10.0);

            var stats = store.Get(Alice);
            Assert.That(stats.Flights, Is.EqualTo(2));
            Assert.That(stats.FlightTime, Is.EqualTo(15.0));
        }

        [Test]
        public void Takeover_EndsOneFlightAndStartsAnother()
        {
            recorder.FlightStarted(Alice, Drone1, 0.0);

            recorder.Preempted(Alice);
            recorder.FlightEnded(Alice, Drone1, 8.0);
            recorder.TookOver(Bob);
            recorder.FlightStarted(Bob, Drone1, 8.0);

            var alice = store.Get(Alice);
            var bob = store.Get(Bob);
            Assert.That(alice.TimesPreempted, Is.EqualTo(1));
            Assert.That(alice.FlightTime, Is.EqualTo(8.0));
            Assert.That(bob.Takeovers, Is.EqualTo(1));
            Assert.That(bob.Flights, Is.EqualTo(1));
            Assert.That(recorder.IsFlying(Bob, Drone1), Is.True);
        }

        [Test]
        public void StartingTheSameFlightTwice_Throws()
        {
            recorder.FlightStarted(Alice, Drone1, 0.0);

            Assert.Throws<InvalidOperationException>(() => recorder.FlightStarted(Alice, Drone1, 1.0));
        }

        [Test]
        public void EndingAFlightThatNeverStarted_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => recorder.FlightEnded(Alice, Drone1, 1.0));
        }

        [Test]
        public void EndingBeforeStart_Throws()
        {
            recorder.FlightStarted(Alice, Drone1, 10.0);

            Assert.Throws<ArgumentOutOfRangeException>(() => recorder.FlightEnded(Alice, Drone1, 9.0));
            Assert.That(recorder.IsFlying(Alice, Drone1), Is.True);
        }

        [Test]
        public void EmptyIds_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => recorder.FlightStarted(default, Drone1, 0.0));
            Assert.Throws<ArgumentException>(() => recorder.FlightStarted(Alice, default, 0.0));
            Assert.Throws<ArgumentException>(() => recorder.TookOver(default));
            Assert.Throws<ArgumentException>(() => recorder.Preempted(default));
            Assert.Throws<ArgumentException>(() => store.Save(default, PilotStats.None));
        }
    }
}
