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
        private DroneRegistry registry;
        private FlightCoordinator coordinator;
        private List<DroneAssigned> assigned;
        private List<DroneReleased> released;
        private List<DroneTakenOver> tookOver;
        private List<DroneMoved> moved;
        private List<FlightDenied> denied;

        [SetUp]
        public void SetUp()
        {
            provider = new ScriptedAuthorizationProvider("test-provider");
            airborne = new TestDrone("drone-1", isAirborne: true);
            grounded = new TestDrone("drone-2", isAirborne: false);
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
            moved = new List<DroneMoved>();
            denied = new List<FlightDenied>();
            coordinator.Assigned += assigned.Add;
            coordinator.Released += released.Add;
            coordinator.TookOver += tookOver.Add;
            coordinator.Moved += moved.Add;
            coordinator.Denied += denied.Add;
        }

        private PilotSession Session(PilotId pilot, IAuthorizationProvider authorizer = null)
        {
            return new PilotSession(pilot, authorizer ?? provider, new ScriptedInput());
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
            Assert.Throws<ArgumentNullException>(() => coordinator.CancelRequest(null));
            Assert.Throws<ArgumentNullException>(() => coordinator.EndSession(null));
            Assert.Throws<ArgumentNullException>(() => coordinator.Reroute(null));
            Assert.Throws<ArgumentNullException>(() => Session(Alice).MoveTo(null));
            Assert.Throws<ArgumentException>(() => new PilotSession(default, provider, new ScriptedInput()));
            Assert.Throws<ArgumentNullException>(() => new PilotSession(Alice, null, new ScriptedInput()));
            Assert.Throws<ArgumentNullException>(() => new PilotSession(Alice, provider, null));
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

            coordinator.CancelRequest(session);
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
            var late = new TestDrone("drone-3", isAirborne: false);

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
            var late = new TestDrone("drone-3", isAirborne: false);

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
        public void AnyDroneWhileFlying_KeepsTheCurrentOne()
        {
            Request(Session(Bob), airborne.Id);
            var session = Session(Carol);
            Request(session, grounded.Id);

            var result = Request(session);

            Assert.That(result.Outcome, Is.EqualTo(FlightRequestOutcome.Assigned));
            Assert.That(result.Drone, Is.EqualTo(grounded.Id));
            AssertFlying(Carol, grounded);
            AssertFlying(Bob, airborne);
            Assert.That(released, Is.Empty);
            Assert.That(tookOver, Is.Empty);
        }

        [Test]
        public void ASecondSessionForAPilot_MovesTheirOneDrone()
        {
            var local = Session(Alice);
            Request(local, airborne.Id);
            var pad = new PilotSession(Alice, provider, new ScriptedInput());

            Request(pad, grounded.Id);

            AssertFlying(Alice, grounded);
            AssertFree(airborne);
            Assert.That(coordinator.Release(local), Is.False, "The local session no longer routes a drone.");
            Assert.That(coordinator.Release(pad), Is.True);
        }

        [Test]
        public void AThrowingListener_IsReported_AndTheSwitchStillCompletes()
        {
            var failures = new List<Exception>();
            coordinator.ListenerFailed += failures.Add;
            var session = Session(Alice);
            Request(session, airborne.Id);
            coordinator.Released += _ => throw new InvalidOperationException("listener failed");
            int heardAfterTheThrow = 0;
            coordinator.Released += _ => heardAfterTheThrow++;

            var result = Request(session, grounded.Id);

            Assert.That(result.Drone, Is.EqualTo(grounded.Id));
            AssertFlying(Alice, grounded);
            AssertFree(airborne);
            Assert.That(failures, Has.Count.EqualTo(1));
            Assert.That(heardAfterTheThrow, Is.EqualTo(1));
            Assert.That(assigned, Has.Count.EqualTo(2), "The switch still raises its assignment.");
        }

        [Test]
        public void AThrowingListener_DuringRevocation_DoesNotReachTheRevoker()
        {
            var ledger = new GrantLedger("ledger-provider");
            var session = Session(Alice, new LedgerProvider(ledger));
            Request(session, airborne.Id);
            coordinator.Released += _ => throw new InvalidOperationException("listener failed");

            Assert.DoesNotThrow(() => ledger.Revoke(Alice));
            AssertFree(airborne);
        }

        [Test]
        public void TheEventsOfASwitch_SeeItComplete()
        {
            var session = Session(Alice);
            Request(session, airborne.Id);
            bool sawTheNewDrone = false;
            coordinator.Released += _ => sawTheNewDrone = coordinator.TryGetDrone(Alice, out var drone) && drone == grounded.Id;

            Request(session, grounded.Id);

            Assert.That(sawTheNewDrone, Is.True);
        }

        [Test]
        public void CancelRequest_EndsThePendingRequestQuietly()
        {
            provider.AnswersAtOnce = false;
            var session = Session(Alice);
            var pending = Start(session);

            Assert.That(coordinator.CancelRequest(session), Is.True);
            provider.AnswerAll();

            Assert.That(pending.IsCanceled, Is.True);
            Assert.That(coordinator.CancelRequest(session), Is.False);
            Assert.That(assigned, Is.Empty);
            Assert.That(denied, Is.Empty);
            AssertFree(airborne);
            AssertFree(grounded);
        }

        [Test]
        public void TheCurrentDrone_IsRoutedToTheNewSession()
        {
            var first = Session(Alice);
            Request(first, airborne.Id);
            var pad = new ScriptedInput { Current = new ControlInput(Vec3.Forward, 0f) };
            var second = new PilotSession(Alice, provider, pad);

            var result = Request(second, airborne.Id);

            Assert.That(result.Outcome, Is.EqualTo(FlightRequestOutcome.Assigned));
            Assert.That(airborne.Router.Read().Move, Is.EqualTo(Vec3.Forward));
            Assert.That(assigned, Has.Count.EqualTo(1), "The flight goes on, so nothing new starts.");

            Assert.That(moved, Is.Empty, "Moved is for a session changing device, not a new session.");
            Assert.That(coordinator.Release(first), Is.False, "The first session no longer steers it.");
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
        public void Reroute_MovesTheDroneToTheSessionsNewInput_WithTheFlightGoingOn()
        {
            var session = new PilotSession(Alice, provider, new ScriptedInput());
            Request(session, airborne.Id);
            var pad = new ScriptedInput { Current = new ControlInput(Vec3.Forward, 0f) };

            session.MoveTo(pad);
            coordinator.Reroute(session);

            Assert.That(airborne.Router.Read().Move, Is.EqualTo(Vec3.Forward));
            AssertFlying(Alice, airborne);
            Assert.That(assigned, Has.Count.EqualTo(1), "The flight goes on, so nothing new starts.");
            Assert.That(released, Is.Empty);
            Assert.That(moved, Has.Count.EqualTo(1));
            Assert.That(moved[0].Session, Is.SameAs(session));
        }

        [Test]
        public void Reroute_OfASessionWithNoDrone_DoesNothing()
        {
            coordinator.Reroute(Session(Alice));

            Assert.That(moved, Is.Empty);
        }

        [Test]
        public void Release_AlsoCancelsThePendingRequest()
        {
            var session = Session(Alice);
            Request(session, airborne.Id);
            provider.AnswersAtOnce = false;
            var pending = Start(session, grounded.Id);

            coordinator.Release(session);
            provider.AnswerAll();

            Assert.That(pending.IsCanceled, Is.True);
            AssertFree(airborne);
            AssertFree(grounded);
        }

        [Test]
        public void ADisconnectedPilot_IsDenied_AndTakesNothingOver()
        {
            var flown = Session(Bob);
            Request(flown, airborne.Id);
            var unplugged = new PilotSession(Alice, provider, new ScriptedInput { IsConnected = false });

            var result = Request(unplugged, airborne.Id);

            Assert.That(result.Denial, Is.EqualTo(FlightDenial.PilotDisconnected));
            AssertFlying(Bob, airborne);
            Assert.That(tookOver, Is.Empty);
        }

        [Test]
        public void ALinkLostDuringAuthorization_IsDenied()
        {
            var input = new ScriptedInput();
            var session = new PilotSession(Alice, provider, input);
            provider.AnswersAtOnce = false;
            var pending = Start(session, airborne.Id);

            input.IsConnected = false;
            provider.AnswerAll();

            Assert.That(pending.Result.Denial, Is.EqualTo(FlightDenial.PilotDisconnected));
            AssertFree(airborne);
        }

        [Test]
        public void ADroneThatLandedWithoutItsPilot_IsReleased()
        {
            var input = new ScriptedInput();
            Request(new PilotSession(Alice, provider, input), grounded.Id);

            coordinator.ReleaseLandedWithoutPilot();
            AssertFlying(Alice, grounded);

            input.IsConnected = false;
            coordinator.ReleaseLandedWithoutPilot();

            AssertFree(grounded);
            Assert.That(released[0].Reason, Is.EqualTo(ReleaseReason.LinkLost));
        }

        [Test]
        public void AnAirborneDroneWithoutItsPilot_StaysAssignedForTheFailsafe()
        {
            var input = new ScriptedInput();
            Request(new PilotSession(Alice, provider, input), airborne.Id);

            input.IsConnected = false;
            coordinator.ReleaseLandedWithoutPilot();

            Assert.That(coordinator.TryGetDrone(Alice, out _), Is.True);
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

        private sealed class LedgerProvider : IAuthorizationProvider
        {
            private readonly GrantLedger grants;

            public LedgerProvider(GrantLedger grants)
            {
                this.grants = grants;
            }

            public string Id => "ledger-provider";

            public Task<AuthorizationResult> AuthorizeAsync(AuthorizationRequest request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new AuthorizationResult(grants.Issue(request.PilotId)));
            }
        }

        // In Unity the answer is used a frame after it arrives, so the grant can be revoked in between.
        [Test]
        public void Authorization_StartsUnauthorized()
        {
            Assert.That(coordinator.AuthorizationOf(Alice), Is.EqualTo(AuthorizationState.Unauthorized));
        }

        [Test]
        public void Authorization_IsPendingWhileTheProviderAnswers_ThenGranted()
        {
            provider.AnswersAtOnce = false;
            Start(Session(Alice));
            Assert.That(coordinator.AuthorizationOf(Alice), Is.EqualTo(AuthorizationState.Pending));

            provider.AnswerAll();

            Assert.That(coordinator.AuthorizationOf(Alice), Is.EqualTo(AuthorizationState.Granted));
        }

        [Test]
        public void Authorization_IsGranted_WhenTheAssignedEventIsRaised()
        {
            var seen = new List<AuthorizationState>();
            coordinator.Assigned += change => seen.Add(coordinator.AuthorizationOf(change.Pilot));

            Request(Session(Alice));

            Assert.That(seen, Is.EqualTo(new[] { AuthorizationState.Granted }));
        }

        [Test]
        public void Authorization_IsDenied_WhenTheProviderRefuses()
        {
            provider.IsApproving = false;

            Request(Session(Alice));

            Assert.That(coordinator.AuthorizationOf(Alice), Is.EqualTo(AuthorizationState.Denied));
        }

        [Test]
        public void Authorization_IsRevoked_WhenTheGrantIsRevokedInFlight()
        {
            Request(Session(Alice));

            provider.Revoke(Alice);

            Assert.That(coordinator.AuthorizationOf(Alice), Is.EqualTo(AuthorizationState.Revoked));
        }

        [Test]
        public void Authorization_IsRevoked_WhenTheGrantIsRevokedBeforeItIsUsed()
        {
            Request(Session(Alice, new RevokedGrantProvider()));

            Assert.That(coordinator.AuthorizationOf(Alice), Is.EqualTo(AuthorizationState.Revoked));
        }

        [Test]
        public void Authorization_GoesBackToUnauthorized_WhenTheRequestIsCancelled()
        {
            provider.AnswersAtOnce = false;
            var session = Session(Alice);
            Start(session);

            coordinator.CancelRequest(session);

            Assert.That(coordinator.AuthorizationOf(Alice), Is.EqualTo(AuthorizationState.Unauthorized));
        }

        [Test]
        public void Authorization_GoesBackToUnauthorized_WhenTheCallerCancels()
        {
            provider.AnswersAtOnce = false;
            using (var caller = new CancellationTokenSource())
            {
                var request = coordinator.RequestFlightAsync(Session(Alice), null, caller.Token);

                caller.Cancel();

                Assert.That(request.IsCanceled, Is.True);
            }
            Assert.That(coordinator.AuthorizationOf(Alice), Is.EqualTo(AuthorizationState.Unauthorized));
        }

        [Test]
        public void Authorization_GoesBackToUnauthorized_WhenNoDroneIsLeftForTheGrant()
        {
            Request(Session(Bob), airborne.Id);

            Request(Session(Alice), airborne.Id);

            Assert.That(coordinator.AuthorizationOf(Alice), Is.EqualTo(AuthorizationState.Unauthorized));
        }

        [Test]
        public void Authorization_GoesBackToUnauthorized_WhenTheDroneIsReleased()
        {
            var session = Session(Alice);
            Request(session);

            coordinator.Release(session);

            Assert.That(coordinator.AuthorizationOf(Alice), Is.EqualTo(AuthorizationState.Unauthorized));
        }

        [Test]
        public void Authorization_MovesToTheNewPilot_OnATakeover()
        {
            Request(Session(Alice), airborne.Id);

            Request(Session(Bob), airborne.Id);

            Assert.That(coordinator.AuthorizationOf(Alice), Is.EqualTo(AuthorizationState.Unauthorized));
            Assert.That(coordinator.AuthorizationOf(Bob), Is.EqualTo(AuthorizationState.Granted));
        }

        [Test]
        public void Authorization_StaysGranted_WhileAFlyingPilotAsksForAnotherDrone()
        {
            var session = Session(Alice);
            Request(session, airborne.Id);
            provider.AnswersAtOnce = false;

            Start(session, grounded.Id);

            Assert.That(coordinator.AuthorizationOf(Alice), Is.EqualTo(AuthorizationState.Granted));
        }

        [Test]
        public void Authorization_StaysGranted_WhenAFlyingPilotIsRefusedAnotherDrone()
        {
            var session = Session(Alice);
            Request(session, airborne.Id);
            provider.IsApproving = false;

            Request(session, grounded.Id);

            Assert.That(coordinator.AuthorizationOf(Alice), Is.EqualTo(AuthorizationState.Granted));
        }

        [Test]
        public void Authorization_StaysPending_WhenTheDroneGoesDuringANewRequest()
        {
            var session = Session(Alice);
            Request(session, airborne.Id);
            provider.AnswersAtOnce = false;
            Start(session, grounded.Id);

            coordinator.RemoveDrone(airborne.Id);

            Assert.That(coordinator.AuthorizationOf(Alice), Is.EqualTo(AuthorizationState.Pending));
        }

        [Test]
        public void EndingASession_ClearsItsAuthorization()
        {
            provider.IsApproving = false;
            var session = Session(Alice);
            Request(session);

            coordinator.EndSession(session);

            Assert.That(coordinator.AuthorizationOf(Alice), Is.EqualTo(AuthorizationState.Unauthorized));
        }

        [Test]
        public void ToggleArm_WithAGrant_ArmsTheDrone()
        {
            var session = Session(Alice);
            Request(session, grounded.Id);

            var result = coordinator.ToggleArm(session);

            Assert.That(result.IsArmed, Is.True);
            Assert.That(grounded.Arming.IsArmed, Is.True);
        }

        [Test]
        public void ToggleArm_WithNoDrone_IsRefused_AndRaisesDenied()
        {
            var result = coordinator.ToggleArm(Session(Alice));

            Assert.That(result.IsArmed, Is.False);
            StringAssert.Contains("can't arm: Unauthorized", result.Message);
            Assert.That(denied[0].Denial, Is.EqualTo(FlightDenial.NotArmed));
            Assert.That(denied[0].Preferred, Is.Null);
        }

        [Test]
        public void ToggleArm_WhileARequestIsPending_SaysPending()
        {
            provider.AnswersAtOnce = false;
            var session = Session(Alice);
            Start(session, grounded.Id);

            var result = coordinator.ToggleArm(session);

            StringAssert.Contains("can't arm: Pending", result.Message);
            Assert.That(grounded.Arming.IsArmed, Is.False);
        }

        [Test]
        public void ToggleArm_AfterRevocation_SaysRevoked()
        {
            var session = Session(Alice);
            Request(session, grounded.Id);
            provider.Revoke(Alice);

            StringAssert.Contains("can't arm: Revoked", coordinator.ToggleArm(session).Message);
        }

        [Test]
        public void ToggleArm_OnTheGround_Disarms()
        {
            var session = Session(Alice);
            Request(session, grounded.Id);
            coordinator.ToggleArm(session);

            var result = coordinator.ToggleArm(session);

            Assert.That(result.IsArmed, Is.False);
            Assert.That(grounded.Arming.IsArmed, Is.False);
        }

        [Test]
        public void ToggleArm_InTheAir_IsRefused()
        {
            var session = Session(Alice);
            Request(session, airborne.Id);
            airborne.Tick();

            var result = coordinator.ToggleArm(session);

            StringAssert.Contains("in the air", result.Message);
            AssertFlying(Alice, airborne);
            Assert.That(airborne.Gate.IsFlightAllowed, Is.True);
        }

        [Test]
        public void Releasing_ClearsTheSwitch()
        {
            var session = Session(Alice);
            Request(session, grounded.Id);
            coordinator.ToggleArm(session);

            coordinator.Release(session);

            Assert.That(grounded.Arming.IsArmed, Is.False);
        }

        [Test]
        public void AGroundedTakeover_LeavesTheDroneDisarmed()
        {
            var alice = Session(Alice);
            Request(alice, grounded.Id);
            coordinator.ToggleArm(alice);

            Request(Session(Bob), grounded.Id);

            Assert.That(grounded.Arming.IsArmed, Is.False);
        }

        [Test]
        public void RevokedOnTheGround_ThenGrantedAgain_StaysDisarmed()
        {
            var session = Session(Alice);
            Request(session, grounded.Id);
            coordinator.ToggleArm(session);
            provider.Revoke(Alice);

            Request(session, grounded.Id);

            Assert.That(grounded.Arming.IsArmed, Is.False);
        }

        [Test]
        public void RequestingYourOwnDrone_KeepsItArmed()
        {
            var session = Session(Alice);
            Request(session, grounded.Id);
            coordinator.ToggleArm(session);

            Request(session, grounded.Id);

            Assert.That(grounded.Arming.IsArmed, Is.True);
        }

        [Test]
        public void Reroute_KeepsItArmed()
        {
            var session = Session(Alice);
            Request(session, grounded.Id);
            coordinator.ToggleArm(session);
            session.MoveTo(new ScriptedInput());

            coordinator.Reroute(session);

            Assert.That(grounded.Arming.IsArmed, Is.True);
        }

        [Test]
        public void AnAirborneTakeover_KeepsFlying()
        {
            Request(Session(Alice), airborne.Id);
            airborne.Tick();

            Request(Session(Bob), airborne.Id);
            airborne.Tick();

            Assert.That(airborne.Gate.IsFlightAllowed, Is.True);
            Assert.That(airborne.Loop.Mode, Is.EqualTo(FlightMode.Piloted));
        }

        private sealed class RevokedGrantProvider : IAuthorizationProvider
        {
            private readonly GrantLedger grants = new GrantLedger("revoking-provider");

            public string Id => "revoking-provider";

            public Task<AuthorizationResult> AuthorizeAsync(AuthorizationRequest request, CancellationToken cancellationToken)
            {
                var grant = grants.Issue(request.PilotId);
                grants.Revoke(request.PilotId);
                return Task.FromResult(new AuthorizationResult(grant));
            }
        }
    }
}
