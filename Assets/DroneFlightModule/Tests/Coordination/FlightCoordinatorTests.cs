using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DroneFlightModule.Authorization;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;
using NUnit.Framework;

namespace DroneFlightModule.Coordination.Tests
{
    public class FlightCoordinatorTests : SynchronousAsyncFixture
    {
        private static readonly PilotId Alice = new PilotId("alice");
        private static readonly PilotId Carol = new PilotId("carol");
        private static readonly PilotId Dave = new PilotId("dave");
        private static readonly PilotId Bob = new PilotId("bob");
        private static readonly PilotId Stranger = new PilotId("stranger");

        private ScriptedAuthorizationProvider provider;
        private TestDrone airborne;
        private TestDrone grounded;
        private PilotClaims claims;
        private DroneRegistry registry;
        private FlightCoordinator coordinator;
        private List<DroneAssigned> assigned;
        private List<DroneReleased> released;
        private List<DroneTakenOver> tookOver;
        private List<FlightDenied> denied;

        [SetUp]
        public void SetUp()
        {
            provider = new ScriptedAuthorizationProvider("test-provider");
            claims = new PilotClaims();
            airborne = new TestDrone("drone-1", claims, isAirborne: true);
            grounded = new TestDrone("drone-2", claims, isAirborne: false);
            var pilots = new InMemoryPilotRegistry(new[]
            {
                new PilotProfile(Alice, "Alice", 10),
                new PilotProfile(Dave, "Dave", 10),
                new PilotProfile(Carol, "Carol", 15),
                new PilotProfile(Bob, "Bob", 20)
            });
            coordinator = new FlightCoordinator(
                pilots,
                registry = new DroneRegistry(new[] { airborne.Fleet, grounded.Fleet }),
                new PriorityAssignmentPolicy());

            assigned = new List<DroneAssigned>();
            released = new List<DroneReleased>();
            tookOver = new List<DroneTakenOver>();
            denied = new List<FlightDenied>();
            coordinator.Assigned += assigned.Add;
            coordinator.Released += released.Add;
            coordinator.TookOver += tookOver.Add;
            coordinator.Denied += denied.Add;
        }

        private PilotSession Session(PilotId pilot, IAuthorizationProvider authorizer = null)
        {
            return new PilotSession(pilot, PilotOrigin.Local, authorizer ?? provider, new ScriptedInput());
        }

        private Task<FlightRequestResult> Start(PilotSession session, DroneId? preferred = null)
        {
            return coordinator.RequestFlightAsync(session, preferred, CancellationToken.None);
        }

        private FlightRequestResult Request(PilotSession session, DroneId? preferred = null)
        {
            var task = Start(session, preferred);
            Assert.That(task.IsCompleted, Is.True, "The request should finish without waiting.");
            return task.Result;
        }

        private void AssertFlying(PilotId pilot, TestDrone drone)
        {
            Assert.That(coordinator.TryGetDrone(pilot, out var flown), Is.True, $"{pilot} should be flying.");
            Assert.That(flown, Is.EqualTo(drone.Id));
            Assert.That(drone.Router.ActivePilotId, Is.EqualTo(pilot));
            Assert.That(drone.Holder.Grant.PilotId, Is.EqualTo(pilot));
            Assert.That(drone.Gate.IsFlightAllowed, Is.True);
        }

        private void AssertFree(TestDrone drone)
        {
            Assert.That(coordinator.TryGetPilot(drone.Id, out _), Is.False, $"{drone.Id} should be free.");
            Assert.That(drone.Router.ActivePilotId.IsEmpty, Is.True);
            Assert.That(drone.Holder.Grant, Is.Null);
            Assert.That(drone.Gate.IsFlightAllowed, Is.False);
        }

        [Test]
        public void Arguments_AreCheckedBeforeAnythingStarts()
        {
            Assert.Throws<ArgumentNullException>(() => Start(null));
            Assert.Throws<ArgumentException>(() => Start(Session(Alice), default(DroneId)));
            Assert.Throws<ArgumentNullException>(() => coordinator.Release(null));
            Assert.Throws<ArgumentNullException>(() => coordinator.EndSession(null));
            Assert.Throws<ArgumentException>(() => new PilotSession(default, PilotOrigin.Local, provider, new ScriptedInput()));
            Assert.Throws<ArgumentNullException>(() => new PilotSession(Alice, PilotOrigin.Local, null, new ScriptedInput()));
            Assert.Throws<ArgumentNullException>(() => new PilotSession(Alice, PilotOrigin.Local, provider, null));
        }

        [Test]
        public void UnknownPilot_IsDeniedWithoutAskingTheProvider()
        {
            var result = Request(Session(Stranger));

            Assert.That(result.Outcome, Is.EqualTo(FlightRequestOutcome.Denied));
            Assert.That(result.Denial, Is.EqualTo(FlightDenial.UnknownPilot));
            Assert.That(provider.RequestCount, Is.EqualTo(0));
        }

        [Test]
        public void EveryDenial_IsAnEvent_AndASuccessIsNot()
        {
            Request(Session(Stranger));
            Request(Session(Alice), new DroneId("drone-9"));
            Request(Session(Alice));

            Assert.That(denied, Has.Count.EqualTo(2));
            Assert.That(denied[0].Pilot, Is.EqualTo(Stranger));
            Assert.That(denied[0].Denial, Is.EqualTo(FlightDenial.UnknownPilot));
            Assert.That(denied[1].Preferred, Is.EqualTo(new DroneId("drone-9")));
            Assert.That(denied[1].Denial, Is.EqualTo(FlightDenial.UnknownDrone));
            Assert.That(denied[1].Message, Is.EqualTo("drone-9 is not in the fleet."));
        }

        [Test]
        public void ACancelledRequest_IsNoDenial()
        {
            var session = Session(Alice);
            provider.AnswersAtOnce = false;
            Start(session);

            coordinator.EndSession(session);
            provider.AnswerAll();

            Assert.That(denied, Is.Empty);
        }

        [Test]
        public void ProviderRefusal_IsDeniedAsNotAuthorized()
        {
            provider.IsApproving = false;

            var result = Request(Session(Alice));

            Assert.That(result.Denial, Is.EqualTo(FlightDenial.NotAuthorized));
            Assert.That(result.Message, Is.EqualTo("The test provider denies everyone."));
            AssertFree(airborne);
            AssertFree(grounded);
        }

        [Test]
        public void AnyDrone_GetsAFreeOne_PreferringGrounded()
        {
            var result = Request(Session(Alice));

            Assert.That(result.Outcome, Is.EqualTo(FlightRequestOutcome.Assigned));
            Assert.That(result.Drone, Is.EqualTo(grounded.Id));
            AssertFlying(Alice, grounded);
            Assert.That(assigned, Has.Count.EqualTo(1));
            Assert.That(assigned[0].Pilot, Is.EqualTo(Alice));
        }

        [Test]
        public void PreferredFreeDrone_IsAssigned()
        {
            var result = Request(Session(Alice), airborne.Id);

            Assert.That(result.Drone, Is.EqualTo(airborne.Id));
            AssertFlying(Alice, airborne);
        }

        [Test]
        public void UnknownPreferredDrone_IsDenied()
        {
            var result = Request(Session(Alice), new DroneId("drone-9"));

            Assert.That(result.Denial, Is.EqualTo(FlightDenial.UnknownDrone));
        }

        [Test]
        public void HigherPriority_TakesOverWithoutTheGateClosing()
        {
            Request(Session(Alice), airborne.Id);
            airborne.Tick();
            Assert.That(airborne.Loop.Mode, Is.EqualTo(FlightMode.Piloted));

            var result = Request(Session(Bob), airborne.Id);

            Assert.That(result.Outcome, Is.EqualTo(FlightRequestOutcome.TookOver));
            Assert.That(result.FromPilot, Is.EqualTo(Alice));
            AssertFlying(Bob, airborne);
            Assert.That(coordinator.TryGetDrone(Alice, out _), Is.False);
            airborne.Tick();
            Assert.That(airborne.Loop.Mode, Is.EqualTo(FlightMode.Piloted));
            Assert.That(tookOver, Has.Count.EqualTo(1));
            Assert.That(tookOver[0].FromPilot, Is.EqualTo(Alice));
            Assert.That(released, Is.Empty, "A takeover is not a release.");
        }

        [Test]
        public void NoFreeDrone_TakesOverTheLowestPriority()
        {
            Request(Session(Alice), airborne.Id);
            Request(Session(Carol), grounded.Id);

            var result = Request(Session(Bob));

            Assert.That(result.Outcome, Is.EqualTo(FlightRequestOutcome.TookOver));
            AssertFlying(Bob, airborne);
            AssertFlying(Carol, grounded);
        }

        [Test]
        public void EqualPriority_NeverTakesOver()
        {
            Request(Session(Alice), airborne.Id);
            Request(Session(Carol), grounded.Id);

            Assert.That(Request(Session(Dave)).Denial, Is.EqualTo(FlightDenial.NoDroneAvailable));
            Assert.That(Request(Session(Dave), airborne.Id).Denial, Is.EqualTo(FlightDenial.NoDroneAvailable));
            AssertFlying(Alice, airborne);
        }

        [Test]
        public void ADroneThatJoinsLater_IsAssignedLikeAnyOther()
        {
            Request(Session(Alice), airborne.Id);
            Request(Session(Carol), grounded.Id);
            Assert.That(Request(Session(Dave)).Denial, Is.EqualTo(FlightDenial.NoDroneAvailable));
            var late = new TestDrone("drone-3", claims, isAirborne: false);

            registry.Add(late.Fleet);
            var result = Request(Session(Dave));

            Assert.That(result.Drone, Is.EqualTo(late.Id));
            AssertFlying(Dave, late);
        }

        [Test]
        public void ADroneThatJoinsDuringAuthorization_IsPartOfTheDecision()
        {
            Request(Session(Alice), airborne.Id);
            Request(Session(Carol), grounded.Id);
            var slow = new ScriptedAuthorizationProvider("slow-provider") { AnswersAtOnce = false };
            var pending = Start(Session(Dave, slow));
            var late = new TestDrone("drone-3", claims, isAirborne: false);

            registry.Add(late.Fleet);
            slow.AnswerAll();

            Assert.That(pending.Result.Drone, Is.EqualTo(late.Id));
        }

        [Test]
        public void RemovingAFreeDrone_TakesItOutOfTheFleet()
        {
            Assert.That(coordinator.RemoveDrone(airborne.Id), Is.True);

            Assert.That(registry.TryGet(airborne.Id, out _), Is.False);
            Assert.That(registry.Drones, Has.Count.EqualTo(1));
            Assert.That(coordinator.RemoveDrone(airborne.Id), Is.False);
            Assert.That(released, Is.Empty);
        }

        [Test]
        public void RemovingAFlownDrone_ReleasesItFirst()
        {
            Request(Session(Alice), airborne.Id);

            coordinator.RemoveDrone(airborne.Id);

            Assert.That(released, Has.Count.EqualTo(1));
            Assert.That(released[0].Reason, Is.EqualTo(ReleaseReason.Removed));
            AssertFree(airborne);
            Assert.That(coordinator.TryGetDrone(Alice, out _), Is.False);
            Assert.That(Request(Session(Alice), grounded.Id).Drone, Is.EqualTo(grounded.Id), "Her claim went with the drone.");
        }

        [Test]
        public void APendingRequestForARemovedDrone_IsDeniedAsUnknown()
        {
            var slow = new ScriptedAuthorizationProvider("slow-provider") { AnswersAtOnce = false };
            var pending = Start(Session(Alice, slow), airborne.Id);

            coordinator.RemoveDrone(airborne.Id);
            slow.AnswerAll();

            Assert.That(pending.Result.Denial, Is.EqualTo(FlightDenial.UnknownDrone));
        }

        [Test]
        public void ARemovedDrone_CanRejoin()
        {
            coordinator.RemoveDrone(airborne.Id);

            registry.Add(airborne.Fleet);

            Assert.That(Request(Session(Alice), airborne.Id).Drone, Is.EqualTo(airborne.Id));
        }

        [Test]
        public void Switching_ReleasesTheFirstDroneOnceTheSecondIsSecured()
        {
            Request(Session(Alice), airborne.Id);

            var result = Request(Session(Alice), grounded.Id);

            Assert.That(result.Drone, Is.EqualTo(grounded.Id));
            AssertFlying(Alice, grounded);
            AssertFree(airborne);
            Assert.That(released, Has.Count.EqualTo(1));
            Assert.That(released[0].Drone, Is.EqualTo(airborne.Id));
            Assert.That(released[0].Reason, Is.EqualTo(ReleaseReason.Switched));
        }

        [Test]
        public void DeniedSwitch_KeepsTheCurrentDrone()
        {
            Request(Session(Alice), airborne.Id);
            Request(Session(Bob), grounded.Id);

            var result = Request(Session(Alice), grounded.Id);

            Assert.That(result.Denial, Is.EqualTo(FlightDenial.NoDroneAvailable));
            AssertFlying(Alice, airborne);
            Assert.That(released, Is.Empty);
        }

        [Test]
        public void AnyDroneWhileFlying_LooksOnlyAtTheOthers()
        {
            Request(Session(Alice), grounded.Id);

            var result = Request(Session(Alice));

            Assert.That(result.Drone, Is.EqualTo(airborne.Id));
            AssertFree(grounded);
        }

        [Test]
        public void TheCurrentDrone_IsRoutedToTheNewSession()
        {
            var first = Session(Alice);
            Request(first, airborne.Id);
            var phone = new ScriptedInput { Current = new ControlInput(Vec3.Forward, 0f) };
            var second = new PilotSession(Alice, PilotOrigin.Remote, provider, phone);

            var result = Request(second, airborne.Id);

            Assert.That(result.Outcome, Is.EqualTo(FlightRequestOutcome.Assigned));
            Assert.That(airborne.Router.Read().Move, Is.EqualTo(Vec3.Forward));
            Assert.That(assigned, Has.Count.EqualTo(1), "The flight goes on, so nothing new starts.");

            coordinator.EndSession(first);
            AssertFlying(Alice, airborne);
        }

        [Test]
        public void Revocation_ReleasesTheDrone()
        {
            Request(Session(Alice), airborne.Id);

            provider.Revoke(Alice);

            AssertFree(airborne);
            Assert.That(released[0].Reason, Is.EqualTo(ReleaseReason.Revoked));
        }

        [Test]
        public void RevokingAGrantFromAnEarlierDrone_LeavesTheCurrentOneAlone()
        {
            var other = new ScriptedAuthorizationProvider("other-provider");
            Request(Session(Alice, other), airborne.Id);
            Request(Session(Alice), grounded.Id);

            other.Revoke(Alice);

            AssertFlying(Alice, grounded);
            Assert.That(released, Has.Count.EqualTo(1));
        }

        [Test]
        public void Release_FreesTheDrone()
        {
            var session = Session(Alice);
            Request(session, airborne.Id);

            Assert.That(coordinator.Release(session), Is.True);

            AssertFree(airborne);
            Assert.That(released[0].Reason, Is.EqualTo(ReleaseReason.Released));
            Assert.That(coordinator.Release(session), Is.False);
        }

        [Test]
        public void EndSession_CancelsThePendingRequestAndReleases()
        {
            var session = Session(Alice);
            Request(session, airborne.Id);
            provider.AnswersAtOnce = false;
            var pending = Start(session, grounded.Id);

            coordinator.EndSession(session);
            provider.AnswerAll();

            Assert.That(pending.IsCanceled, Is.True);
            AssertFree(airborne);
            AssertFree(grounded);
            Assert.That(released[0].Reason, Is.EqualTo(ReleaseReason.SessionEnded));
        }

        [Test]
        public void NewRequest_CancelsThePendingOne()
        {
            var session = Session(Alice);
            provider.AnswersAtOnce = false;
            var first = Start(session, airborne.Id);
            var second = Start(session, grounded.Id);

            provider.AnswerAll();

            Assert.That(first.IsCanceled, Is.True);
            Assert.That(second.Result.Drone, Is.EqualTo(grounded.Id));
            AssertFree(airborne);
        }

        [Test]
        public void CancellingTheCallersToken_CancelsTheRequest()
        {
            provider.AnswersAtOnce = false;
            using (var cancellation = new CancellationTokenSource())
            {
                var pending = coordinator.RequestFlightAsync(Session(Alice), null, cancellation.Token);

                cancellation.Cancel();

                Assert.That(pending.IsCanceled, Is.True);
            }
        }

        [Test]
        public void TheDecision_UsesTheFleetAsItIsWhenAuthorizationCompletes()
        {
            var slow = new ScriptedAuthorizationProvider("slow-provider") { AnswersAtOnce = false };
            var pending = Start(Session(Alice, slow), airborne.Id);

            Request(Session(Bob), airborne.Id);
            slow.AnswerAll();

            Assert.That(pending.Result.Denial, Is.EqualTo(FlightDenial.NoDroneAvailable));
            AssertFlying(Bob, airborne);
        }

        [Test]
        public void GrantRevokedBeforeTheAnswerIsUsed_IsDenied()
        {
            var result = Request(Session(Alice, new RevokedGrantProvider()));

            Assert.That(result.Denial, Is.EqualTo(FlightDenial.NotAuthorized));
            AssertFree(airborne);
            AssertFree(grounded);
        }

        // In Unity the answer is used a frame after it arrives, so the grant can be revoked in between.
        private sealed class RevokedGrantProvider : IAuthorizationProvider
        {
            private readonly GrantLedger grants = new GrantLedger("revoking-provider");

            public string Id => "revoking-provider";

            public Task<AuthorizationResult> AuthorizeAsync(AuthorizationRequest request, CancellationToken cancellationToken)
            {
                var grant = grants.Issue(request.PilotId);
                grants.Revoke(request.PilotId);
                return Task.FromResult(AuthorizationResult.Granted(grant));
            }
        }
    }
}
