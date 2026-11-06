using System;
using System.Threading;
using System.Threading.Tasks;
using DroneFlightModule.Authorization;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;
using NUnit.Framework;

namespace DroneFlightModule.Coordination.Tests
{
    public class PilotSeatingTests : SynchronousAsyncFixture
    {
        private static readonly PilotId Ace = new PilotId("ace");
        private static readonly PilotId John = new PilotId("john");
        private static readonly PilotId Guest = PilotProfile.Guest.Id;

        private sealed class OneProvider : IAuthorizationProviderSelector
        {
            private readonly IAuthorizationProvider provider;

            public OneProvider(IAuthorizationProvider provider)
            {
                this.provider = provider;
            }

            public IAuthorizationProvider For(PilotProfile pilot) => provider;
        }

        private ScriptedAuthorizationProvider provider;
        private TestDrone drone;
        private IPilotRegistry pilots;
        private FlightCoordinator coordinator;
        private PilotSeating seating;
        private TestDesk keyboard;
        private TestDesk pad;

        [SetUp]
        public void SetUp()
        {
            provider = new ScriptedAuthorizationProvider("test-provider");
            drone = new TestDrone("drone-1", isAirborne: true);
            pilots = new InMemoryPilotRegistry(new[]
            {
                new PilotProfile(John, "John", 60),
                new PilotProfile(Ace, "Ace", 50)
            });
            coordinator = new FlightCoordinator(pilots, new DroneRegistry(new[] { drone.Fleet }), new PriorityAssignmentPolicy());
            seating = new PilotSeating(coordinator, pilots, new OneProvider(provider));
            keyboard = new TestDesk("Keyboard");
            pad = new TestDesk("Gamepad");
        }

        private Task<FlightRequestResult> Request(TestDesk desk)
        {
            return coordinator.RequestFlightAsync(desk.Session, drone.Id, CancellationToken.None);
        }

        [Test]
        public void Constructor_RequiresItsParts()
        {
            var providers = new OneProvider(provider);

            Assert.Throws<ArgumentNullException>(() => new PilotSeating(null, pilots, providers));
            Assert.Throws<ArgumentNullException>(() => new PilotSeating(coordinator, null, providers));
            Assert.Throws<ArgumentNullException>(() => new PilotSeating(coordinator, pilots, null));
        }

        [Test]
        public void StartPilot_IsTheGuest_UntilSomeoneHasThem()
        {
            Assert.That(seating.StartPilot(), Is.EqualTo(Guest));

            seating.Seat(keyboard, Guest);

            Assert.That(seating.StartPilot().IsEmpty, Is.True);
        }

        [Test]
        public void Seat_GivesThePilotASessionOnTheDesksInput()
        {
            seating.Seat(keyboard, Ace);

            Assert.That(keyboard.Session.PilotId, Is.EqualTo(Ace));
            Assert.That(keyboard.Session.Provider, Is.SameAs(provider));
            Assert.That(keyboard.Session.Input, Is.SameAs(keyboard.Input));
            Assert.That(seating.SeatOf(Ace), Is.EqualTo("Keyboard"));
            StringAssert.Contains("ace sits here", keyboard.LastReport);
        }

        [Test]
        public void Seat_RefusesAnUnknownPilot()
        {
            Assert.Throws<ArgumentException>(() => seating.Seat(keyboard, new PilotId("stranger")));
        }

        [Test]
        public void Seat_AtTheirOwnDesk_ChangesNothing()
        {
            seating.Seat(keyboard, Ace);
            var session = keyboard.Session;

            seating.Seat(keyboard, Ace);

            Assert.That(keyboard.Session, Is.SameAs(session));
        }

        [Test]
        public void ChangingThePilot_ReleasesTheDrone_AndTheNewPilotMustRequest()
        {
            seating.Seat(keyboard, Ace);
            Request(keyboard);

            seating.Seat(keyboard, John);

            Assert.That(coordinator.TryGetDrone(Ace, out _), Is.False);
            Assert.That(coordinator.TryGetDrone(John, out _), Is.False);
            Assert.That(seating.SeatOf(Ace), Is.Empty);
        }

        [Test]
        public void MovingDesks_KeepsTheSessionAndTheDrone_AndEmptiesTheFirstDesk()
        {
            seating.Seat(keyboard, Ace);
            var session = keyboard.Session;
            Request(keyboard);

            seating.Seat(pad, Ace);

            Assert.That(pad.Session, Is.SameAs(session));
            Assert.That(session.Input, Is.SameAs(pad.Input));
            Assert.That(keyboard.Session, Is.Null);
            Assert.That(seating.SeatOf(Ace), Is.EqualTo("Gamepad"));
            Assert.That(coordinator.TryGetDrone(Ace, out var flown), Is.True);
            Assert.That(flown, Is.EqualTo(drone.Id));
            Assert.That(drone.Gate.IsFlightAllowed, Is.True);
            StringAssert.Contains("moved to Gamepad", keyboard.LastReport);
            StringAssert.Contains("moved here", pad.LastReport);
        }

        [Test]
        public void MovingOntoAnOccupiedDesk_SendsItsPilotAway()
        {
            seating.Seat(keyboard, Ace);
            seating.Seat(pad, John);

            seating.Seat(pad, Ace);

            Assert.That(pad.Session.PilotId, Is.EqualTo(Ace));
            Assert.That(seating.SeatOf(John), Is.Empty);
        }

        [Test]
        public void MovingDesks_CancelsAPendingRequest()
        {
            provider.AnswersAtOnce = false;
            seating.Seat(keyboard, Ace);
            var request = Request(keyboard);

            seating.Seat(pad, Ace);

            Assert.That(request.IsCanceled, Is.True);
            StringAssert.Contains("request was cancelled", pad.LastReport);
        }

        [Test]
        public void Vacate_EndsTheSession()
        {
            seating.Seat(keyboard, Ace);
            Request(keyboard);

            seating.Vacate(keyboard);

            Assert.That(keyboard.Session, Is.Null);
            Assert.That(coordinator.TryGetDrone(Ace, out _), Is.False);
            StringAssert.Contains("left the desk", keyboard.LastReport);
        }

        [Test]
        public void Vacate_AnEmptyDesk_DoesNothing()
        {
            Assert.DoesNotThrow(() => seating.Vacate(keyboard));
            Assert.That(keyboard.LastReport, Is.Null);
        }

        [Test]
        public void SeatNext_WalksThePilots_ThenEmpty_ThenRoundAgain()
        {
            seating.SeatNext(keyboard);
            Assert.That(keyboard.Session.PilotId, Is.EqualTo(John));

            seating.SeatNext(keyboard);
            Assert.That(keyboard.Session.PilotId, Is.EqualTo(Ace));

            seating.SeatNext(keyboard);
            seating.SeatNext(keyboard);
            seating.SeatNext(keyboard);
            Assert.That(keyboard.Session.PilotId, Is.EqualTo(John));
        }

        [Test]
        public void SeatNext_SkipsAPilotAtAnotherDesk_AndLeavesTheirFlightAlone()
        {
            seating.Seat(pad, John);
            Request(pad);

            seating.SeatNext(keyboard);

            Assert.That(keyboard.Session.PilotId, Is.EqualTo(Ace));
            Assert.That(pad.Session.PilotId, Is.EqualTo(John));
            Assert.That(coordinator.TryGetDrone(John, out _), Is.True);
        }

        [Test]
        public void SeatNext_LeavesTheDeskEmpty_WhenEveryPilotIsElsewhere()
        {
            seating.Seat(pad, John);
            var other = new TestDesk("Screen panel");
            seating.Seat(other, Ace);
            var third = new TestDesk("Gamepad 2");
            seating.Seat(third, Guest);

            seating.SeatNext(keyboard);

            Assert.That(keyboard.Session, Is.Null);
        }
    }
}
