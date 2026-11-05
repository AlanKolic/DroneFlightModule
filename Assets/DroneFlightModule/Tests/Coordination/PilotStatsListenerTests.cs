using System;
using System.Threading;
using DroneFlightModule.Authorization;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;
using NUnit.Framework;

namespace DroneFlightModule.Coordination.Tests
{
    public class PilotStatsListenerTests : SynchronousAsyncFixture
    {
        private static readonly PilotId Alice = new PilotId("alice");
        private static readonly PilotId Bob = new PilotId("bob");

        private ScriptedAuthorizationProvider provider;
        private TestDrone drone1;
        private TestDrone drone2;
        private FlightCoordinator coordinator;
        private InMemoryPilotStatsStore store;
        private ManualClock clock;
        private PilotStatsListener listener;

        [SetUp]
        public void SetUp()
        {
            provider = new ScriptedAuthorizationProvider("test-provider");
            drone1 = new TestDrone("drone-1", isAirborne: true);
            drone2 = new TestDrone("drone-2", isAirborne: true);
            coordinator = new FlightCoordinator(
                new InMemoryPilotRegistry(new[] { new PilotProfile(Alice, "Alice", 10), new PilotProfile(Bob, "Bob", 20) }),
                new DroneRegistry(new[] { drone1.Fleet, drone2.Fleet }),
                new PriorityAssignmentPolicy());
            store = new InMemoryPilotStatsStore();
            clock = new ManualClock();
            listener = new PilotStatsListener(coordinator, new PilotStatsRecorder(store), clock);
        }

        private PilotSession Fly(PilotId pilot, TestDrone drone)
        {
            var session = new PilotSession(pilot, PilotOrigin.Local, provider, new ScriptedInput());
            var result = coordinator.RequestFlightAsync(session, drone.Id, CancellationToken.None).Result;
            Assert.That(result.IsFlying, Is.True, result.Message);
            return session;
        }

        [Test]
        public void Constructor_RequiresItsParts()
        {
            var recorder = new PilotStatsRecorder(store);

            Assert.Throws<ArgumentNullException>(() => new PilotStatsListener(null, recorder, clock));
            Assert.Throws<ArgumentNullException>(() => new PilotStatsListener(coordinator, null, clock));
            Assert.Throws<ArgumentNullException>(() => new PilotStatsListener(coordinator, recorder, null));
        }

        [Test]
        public void AFlight_IsTimedByTheSimulationClock()
        {
            clock.Time = 2.0;
            var session = Fly(Alice, drone1);
            clock.Time = 7.0;

            coordinator.Release(session);

            var stats = store.Get(Alice);
            Assert.That(stats.Flights, Is.EqualTo(1));
            Assert.That(stats.FlightTime, Is.EqualTo(5.0));
            Assert.That(stats.LastFlight, Is.EqualTo(2.0));
        }

        [Test]
        public void ATakeover_EndsOneFlightAndStartsTheOther()
        {
            clock.Time = 1.0;
            Fly(Alice, drone1);
            clock.Time = 4.0;

            Fly(Bob, drone1);

            Assert.That(store.Get(Alice).FlightTime, Is.EqualTo(3.0));
            Assert.That(store.Get(Alice).TimesPreempted, Is.EqualTo(1));
            Assert.That(store.Get(Bob).Takeovers, Is.EqualTo(1));
            Assert.That(store.Get(Bob).Flights, Is.EqualTo(1));
            Assert.That(store.Get(Bob).LastFlight, Is.EqualTo(4.0));
        }

        [Test]
        public void ASwitch_EndsTheFirstFlightBeforeTheSecondStarts()
        {
            Fly(Alice, drone1);
            clock.Time = 2.0;

            Fly(Alice, drone2);

            Assert.That(store.Get(Alice).Flights, Is.EqualTo(2));
            Assert.That(store.Get(Alice).FlightTime, Is.EqualTo(2.0));
        }

        [Test]
        public void ARevocation_EndsTheFlight()
        {
            Fly(Alice, drone1);
            clock.Time = 3.0;

            provider.Revoke(Alice);

            Assert.That(store.Get(Alice).FlightTime, Is.EqualTo(3.0));
        }

        [Test]
        public void RemovingTheDrone_EndsTheFlight()
        {
            Fly(Alice, drone1);
            clock.Time = 3.0;

            coordinator.RemoveDrone(drone1.Id);

            Assert.That(store.Get(Alice).FlightTime, Is.EqualTo(3.0));
        }

        [Test]
        public void AfterDispose_NothingIsRecorded()
        {
            listener.Dispose();

            Fly(Alice, drone1);

            Assert.That(store.Get(Alice).Flights, Is.EqualTo(0));
        }
    }
}
