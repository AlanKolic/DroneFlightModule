using System;
using System.IO;
using System.Threading;
using DroneFlightModule.Authorization;
using DroneFlightModule.Coordination;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;
using DroneFlightModule.Telemetry;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using VContainer;

namespace DroneFlightModule.Composition.Tests
{
    public class FleetInstallerTests : InputTestFixture
    {
        private const string ControlsPath = "Assets/DroneFlightModule/Unity/Input/DroneControls.inputactions";
        private const float DeltaTime = 0.02f;
        private const float GroundHeight = 1f;

        private static readonly DroneId AirborneDrone = new DroneId("drone-1");
        private static readonly DroneId GroundedDrone = new DroneId("drone-2");

        private Keyboard keyboard;
        private InputActionAsset controls;
        private IObjectResolver fleet;
        private DroneInstantiator drones;
        private IObjectResolver airborne;
        private IObjectResolver grounded;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            controls = InputActionAsset.FromJson(File.ReadAllText(ControlsPath));
            var builder = new ContainerBuilder();
            var installer = new FleetInstaller(GroundHeight, controls, TimeSpan.Zero, CancellationToken.None, null, null, new Vector3(4f, 0f, 0f));
            installer.Install(builder);
            fleet = builder.Build();
            drones = fleet.Resolve<DroneInstantiator>();
            drones.AddDrone(AirborneDrone, new Vector3(0f, 5f, 0f), null);
            drones.AddDrone(GroundedDrone, new Vector3(4f, GroundHeight, 0f), null);
            airborne = Scope(AirborneDrone);
            grounded = Scope(GroundedDrone);
            installer.Start(fleet);
        }

        public override void TearDown()
        {
            fleet?.Dispose();
            UnityEngine.Object.DestroyImmediate(controls);
            base.TearDown();
        }

        private IObjectResolver Scope(DroneId id)
        {
            Assert.That(drones.TryGetScope(id, out var scope), Is.True, $"{id} should be in the fleet.");
            return scope;
        }

        private LocalPilotControls Pilot => fleet.Resolve<LocalPilotControls>();

        private void Run(IObjectResolver drone, float seconds)
        {
            var loop = drone.Resolve<FlightLoop>();
            int ticks = Mathf.RoundToInt(seconds / DeltaTime);
            for (int i = 0; i < ticks; i++)
            {
                loop.Tick(DeltaTime);
            }
        }

        private void Tap(KeyControl key)
        {
            Press(key);
            Release(key);
        }

        private void AssertFlownBy(IObjectResolver drone, PilotId pilot)
        {
            Assert.That(drone.Resolve<InputRouter>().ActivePilotId, Is.EqualTo(pilot));
            Assert.That(drone.Resolve<IFlightGate>().IsFlightAllowed, Is.True);
        }

        private void AssertFree(IObjectResolver drone)
        {
            Assert.That(drone.Resolve<InputRouter>().ActivePilotId.IsEmpty, Is.True);
            Assert.That(drone.Resolve<IFlightGate>().IsFlightAllowed, Is.False);
        }

        [Test]
        public void EachDrone_HasItsOwnScope_AndJoinsTheFleetInOrder()
        {
            Assert.That(airborne.Resolve<FlightLoop>().DroneId, Is.EqualTo(AirborneDrone));
            Assert.That(grounded.Resolve<FlightLoop>().DroneId, Is.EqualTo(GroundedDrone));
            Assert.That(airborne.Resolve<InputRouter>(), Is.Not.SameAs(grounded.Resolve<InputRouter>()));

            var drones = fleet.Resolve<DroneRegistry>().Drones;
            Assert.That(drones, Has.Count.EqualTo(2));
            Assert.That(drones[0].Id, Is.EqualTo(AirborneDrone));
            Assert.That(drones[1].Id, Is.EqualTo(GroundedDrone));
        }

        [Test]
        public void ADroneAddedWhileTheFleetIsInUse_JoinsItAndCanBeFlown()
        {
            Tap(keyboard.rKey);
            var drone3 = new DroneId("drone-3");

            drones.AddDrone(drone3, new Vector3(8f, GroundHeight, 0f), null);
            Tap(keyboard.digit3Key);

            Assert.That(fleet.Resolve<DroneRegistry>().Drones[2].Id, Is.EqualTo(drone3));
            Assert.That(Pilot.TryGetDrone(out var flown), Is.True);
            Assert.That(flown, Is.EqualTo(drone3));
            AssertFlownBy(Scope(drone3), PilotProfile.Guest.Id);
            AssertFree(grounded);
        }

        [Test]
        public void RemovingTheGuestsDrone_ReleasesIt_AndTellsThem()
        {
            Tap(keyboard.rKey);
            var router = grounded.Resolve<InputRouter>();

            drones.RemoveDrone(GroundedDrone);

            Assert.That(Pilot.TryGetDrone(out _), Is.False);
            Assert.That(Pilot.LastMessage, Is.EqualTo("drone-2 was removed from the fleet."));
            Assert.That(fleet.Resolve<DroneRegistry>().Drones, Has.Count.EqualTo(1));
            Assert.That(drones.TryGetScope(GroundedDrone, out _), Is.False);
            Assert.That(router.ActivePilotId.IsEmpty, Is.True);

            Tap(keyboard.rKey);
            AssertFlownBy(airborne, PilotProfile.Guest.Id);
        }

        [Test]
        public void ADroneWhoseIdIsTaken_IsRefused()
        {
            Assert.Throws<ArgumentException>(() => drones.AddDrone(AirborneDrone, new Vector3(0f, 5f, 0f), null));
            Assert.That(fleet.Resolve<DroneRegistry>().Drones, Has.Count.EqualTo(2));
        }

        [Test]
        public void FreeDrones_FlyNobody_AndAnAirborneOneHoldsThenLands()
        {
            AssertFree(airborne);
            AssertFree(grounded);

            Run(airborne, 5f);
            Assert.That(airborne.Resolve<FlightLoop>().Mode, Is.EqualTo(FlightMode.Holding));

            Run(airborne, 25f);
            Assert.That(airborne.Resolve<FlightLoop>().State.IsGrounded, Is.True);
            Assert.That(airborne.Resolve<FlightLoop>().State.Position.Y, Is.EqualTo(GroundHeight));
        }

        [Test]
        public void TheGuest_GetsAGroundedDroneFirst()
        {
            var request = Pilot.RequestFlightAsync(null);

            Assert.That(request.IsCompleted, Is.True);
            Assert.That(Pilot.TryGetDrone(out var drone), Is.True);
            Assert.That(drone, Is.EqualTo(GroundedDrone));
            AssertFlownBy(grounded, PilotProfile.Guest.Id);
            AssertFree(airborne);
        }

        [Test]
        public void TheKeyboard_FliesTheDroneItWasGiven()
        {
            Tap(keyboard.rKey);

            Press(keyboard.spaceKey);
            Run(grounded, 1f);
            Release(keyboard.spaceKey);

            Assert.That(grounded.Resolve<FlightLoop>().State.Position.Y, Is.GreaterThan(GroundHeight + 0.5f));
            Assert.That(grounded.Resolve<FlightLoop>().Mode, Is.EqualTo(FlightMode.Piloted));
        }

        [Test]
        public void NumberKeys_RequestThatDrone_AndSwitchBetweenDrones()
        {
            Tap(keyboard.digit1Key);
            AssertFlownBy(airborne, PilotProfile.Guest.Id);

            Tap(keyboard.digit2Key);
            AssertFlownBy(grounded, PilotProfile.Guest.Id);
            AssertFree(airborne);
        }

        [Test]
        public void ANumberWithoutADrone_IsReported()
        {
            Tap(keyboard.digit3Key);

            Assert.That(Pilot.LastMessage, Is.EqualTo("There is no drone 3."));
            Assert.That(Pilot.TryGetDrone(out _), Is.False);
        }

        [Test]
        public void ReleaseKey_FreesTheDrone()
        {
            Tap(keyboard.rKey);

            Tap(keyboard.xKey);

            AssertFree(grounded);
            Assert.That(Pilot.TryGetDrone(out _), Is.False);
        }

        [Test]
        public void SwitchKey_KeepsThePreviousPilotsDrone_AndTheRegisteredPilotSignsInThroughTheServer()
        {
            Tap(keyboard.rKey);

            Tap(keyboard.pKey);
            AssertFlownBy(grounded, PilotProfile.Guest.Id);
            Assert.That(Pilot.Session.PilotId, Is.EqualTo(FleetInstaller.RegisteredPilot.Id));
            Assert.That(Pilot.LastMessage, Does.EndWith("guest keeps drone-2, hands off."));

            Tap(keyboard.rKey);
            AssertFlownBy(airborne, FleetInstaller.RegisteredPilot.Id);
            Assert.That(airborne.Resolve<DroneGrantHolder>().Grant.ProviderId, Is.EqualTo(SimulatedServerAuthorizationProvider.ProviderId));

            Tap(keyboard.pKey);
            Assert.That(Pilot.Session.PilotId, Is.EqualTo(PilotProfile.Guest.Id));
            Assert.That(Pilot.TryGetDrone(out var drone), Is.True);
            Assert.That(drone, Is.EqualTo(GroundedDrone));
        }

        [Test]
        public void SwitchingAway_CancelsThePreviousPilotsPendingRequest()
        {
            var slowFleet = SlowServerFleet();
            var pilot = slowFleet.Resolve<LocalPilotControls>();
            pilot.SelectPilot(FleetInstaller.RegisteredPilot.Id);
            var ace = pilot.Session;

            var request = pilot.RequestFlightAsync(AirborneDrone);
            Assert.That(request.IsCompleted, Is.False, "The server answers after its latency.");
            pilot.SelectPilot(PilotProfile.Guest.Id);

            Assert.That(pilot.LastMessage, Does.Contain("ace's request was cancelled."));
            Assert.That(slowFleet.Resolve<FlightCoordinator>().CancelRequest(ace), Is.False, "Nothing is left pending.");
            Assert.That(slowFleet.Resolve<FlightCoordinator>().TryGetDrone(ace.PilotId, out _), Is.False);
            slowFleet.Dispose();
        }

        private IObjectResolver SlowServerFleet()
        {
            var builder = new ContainerBuilder();
            new FleetInstaller(GroundHeight, controls, TimeSpan.FromMinutes(1), CancellationToken.None, null, null, new Vector3(4f, 0f, 0f)).Install(builder);
            var slowFleet = builder.Build();
            slowFleet.Resolve<DroneInstantiator>().AddDrone(AirborneDrone, new Vector3(0f, 5f, 0f), null);
            return slowFleet;
        }

        [Test]
        public void DronesAddedAtDifferentTimes_ShareTheFleetsTimebase()
        {
            var clock = fleet.Resolve<SimulationClock>();
            clock.Advance(1f);
            var late = new DroneId("drone-3");
            drones.AddDrone(late, new Vector3(8f, GroundHeight, 0f), null);

            clock.Advance(DeltaTime);
            Scope(late).Resolve<FlightLoop>().Tick(DeltaTime);

            Assert.That(Scope(late).Resolve<FlightLoop>().Time, Is.EqualTo(clock.Time).Within(1e-6));
        }

        [Test]
        public void TheRegisteredPilot_TakesOverTheGuestsDrone_AndBothAreTracked()
        {
            Tap(keyboard.rKey);
            Tap(keyboard.pKey);

            Tap(keyboard.digit2Key);

            AssertFlownBy(grounded, FleetInstaller.RegisteredPilot.Id);
            Assert.That(Pilot.LastMessage, Is.EqualTo("Took over drone-2 from guest."));
            var stats = fleet.Resolve<IPilotStatsStore>();
            Assert.That(stats.Get(FleetInstaller.RegisteredPilot.Id).Takeovers, Is.EqualTo(1));
            Assert.That(stats.Get(PilotProfile.Guest.Id).TimesPreempted, Is.EqualTo(1));

            Tap(keyboard.pKey);
            Assert.That(Pilot.TryGetDrone(out _), Is.False);
        }

        [Test]
        public void TheSelectedPilot_HearsWhenTheyArePreempted()
        {
            Tap(keyboard.rKey);
            var remoteAce = new PilotSession(
                FleetInstaller.RegisteredPilot.Id,
                PilotOrigin.Remote,
                fleet.Resolve<SimulatedServerAuthorizationProvider>(),
                fleet.Resolve<DisconnectableInputProvider>());

            var result = fleet.Resolve<FlightCoordinator>().RequestFlightAsync(remoteAce, GroundedDrone, CancellationToken.None).Result;

            Assert.That(result.Outcome, Is.EqualTo(FlightRequestOutcome.TookOver));
            Assert.That(Pilot.LastMessage, Is.EqualTo("ace took drone-2 over from guest."));
            Assert.That(Pilot.TryGetDrone(out _), Is.False);
        }

        [Test]
        public void AParkedPilotsDrone_IgnoresTheKeyboard()
        {
            Tap(keyboard.rKey);
            Tap(keyboard.pKey);

            Press(keyboard.spaceKey);
            Run(grounded, 1f);
            Release(keyboard.spaceKey);

            Assert.That(grounded.Resolve<FlightLoop>().State.IsGrounded, Is.True);
            AssertFlownBy(grounded, PilotProfile.Guest.Id);
        }

        [Test]
        public void RevokeKey_ReleasesTheRegisteredPilotsDrone()
        {
            Tap(keyboard.pKey);
            Tap(keyboard.rKey);

            Tap(keyboard.vKey);

            AssertFree(grounded);
            Assert.That(Pilot.LastMessage, Does.Contain("revoked"));
        }

        [Test]
        public void SelectingAPilot_SwitchesToThem_AndAnUnknownOneIsRefused()
        {
            Tap(keyboard.rKey);

            Pilot.SelectPilot(FleetInstaller.RegisteredPilot.Id);

            Assert.That(Pilot.Session.PilotId, Is.EqualTo(FleetInstaller.RegisteredPilot.Id));
            AssertFlownBy(grounded, PilotProfile.Guest.Id);
            Assert.Throws<ArgumentException>(() => Pilot.SelectPilot(new PilotId("stranger")));
        }

        [Test]
        public void RevokingAsTheGuest_SaysThereIsNoServerGrant()
        {
            Tap(keyboard.rKey);

            Tap(keyboard.vKey);

            Assert.That(Pilot.LastMessage, Is.EqualTo("guest holds no server grant."));
            AssertFlownBy(grounded, PilotProfile.Guest.Id);
        }

        [Test]
        public void TheConsole_DoesWhatTheKeysDo()
        {
            var console = fleet.Resolve<IFleetConsole>();

            console.SelectPilot(FleetInstaller.RegisteredPilot.Id);
            console.RequestFlight(AirborneDrone);
            Assert.That(console.Pilot, Is.EqualTo(FleetInstaller.RegisteredPilot.Id));
            Assert.That(console.TryGetDrone(out var flown), Is.True);
            Assert.That(flown, Is.EqualTo(AirborneDrone));

            console.ToggleLink();
            Assert.That(console.IsLinkCut, Is.True);
            Assert.That(airborne.Resolve<IFlightGate>().IsFlightAllowed, Is.False);
            console.ToggleLink();

            console.RevokeGrant();
            Assert.That(console.TryGetDrone(out _), Is.False);
            Assert.That(console.LastMessage, Does.Contain("revoked"));

            console.RequestFlight(null);
            console.Release();
            Assert.That(console.TryGetDrone(out _), Is.False);

            console.RemoveDrone(GroundedDrone);
            Assert.That(fleet.Resolve<DroneRegistry>().Drones, Has.Count.EqualTo(1));
        }

        [Test]
        public void LinkLoss_HoldsThenLands()
        {
            Tap(keyboard.digit1Key);

            Tap(keyboard.lKey);
            Run(airborne, 5f);
            Assert.That(airborne.Resolve<FlightLoop>().Mode, Is.EqualTo(FlightMode.Holding));

            Run(airborne, 25f);
            Assert.That(airborne.Resolve<FlightLoop>().State.IsGrounded, Is.True);
        }

        [Test]
        public void StatusChanged_FiresAsTheLocalPilotFliesAndLetsGo()
        {
            int changes = 0;
            Pilot.StatusChanged += () => changes++;

            Tap(keyboard.rKey);
            int afterRequest = changes;
            Tap(keyboard.xKey);

            Assert.That(afterRequest, Is.GreaterThan(0));
            Assert.That(changes, Is.GreaterThan(afterRequest));
        }

        [Test]
        public void Stats_AreRecordedOnTheFleetsSimulationTime()
        {
            var clock = fleet.Resolve<SimulationClock>();
            Tap(keyboard.rKey);

            clock.Advance(3f);
            Tap(keyboard.xKey);

            Assert.That(fleet.Resolve<ISimulationClock>(), Is.SameAs(clock));
            Assert.That(fleet.Resolve<IPilotStatsStore>().Get(PilotProfile.Guest.Id).FlightTime, Is.EqualTo(3.0).Within(1e-6));
        }

        [Test]
        public void EveryDrone_StreamsTelemetryToTheFleetSink()
        {
            Run(airborne, 1f);
            Run(grounded, 1f);

            var sink = fleet.Resolve<InMemoryTelemetrySink>();
            var recorder = airborne.Resolve<TelemetryRecorder>();
            Assert.That(sink.TryGetLatest(AirborneDrone, out var latest), Is.True);
            // 50 ticks of 0.02 s at 10 samples per second: the first tick, then every fifth, so the last at 0.92 s.
            Assert.That(latest.Time, Is.EqualTo(0.92).Within(1e-6));
            Assert.That(latest.Time, Is.EqualTo(recorder.Latest.Time));
            Assert.That(sink.TryGetLatest(GroundedDrone, out _), Is.True);
        }

        [Test]
        public void ARemovedDrone_EndsItsTelemetryStream()
        {
            Run(grounded, 1f);

            drones.RemoveDrone(GroundedDrone);

            Assert.That(fleet.Resolve<InMemoryTelemetrySink>().TryGetLatest(GroundedDrone, out _), Is.False);
        }

        [Test]
        public void Runners_AdvanceOneFixedStep()
        {
            var loop = grounded.Resolve<FlightLoop>();
            var clock = fleet.Resolve<SimulationClock>();

            new FlightLoopRunner(loop).FixedTick();
            new SimulationClockRunner(clock).FixedTick();

            Assert.That(loop.Time, Is.EqualTo(Time.fixedDeltaTime).Within(1e-6));
            Assert.That(clock.Time, Is.EqualTo(Time.fixedDeltaTime).Within(1e-6));
        }

        [Test]
        public void DisposingTheFleet_DisablesTheControls()
        {
            Assert.That(controls.FindActionMap("Flight").enabled, Is.True);
            Assert.That(controls.FindActionMap("Fleet").enabled, Is.True);

            fleet.Dispose();
            fleet = null;

            Assert.That(controls.FindActionMap("Flight").enabled, Is.False);
            Assert.That(controls.FindActionMap("Fleet").enabled, Is.False);
            Assert.That(controls.FindAction("Debug/ToggleLink").enabled, Is.False);
        }
    }
}
