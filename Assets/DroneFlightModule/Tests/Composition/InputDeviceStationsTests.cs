using System;
using System.Collections.Generic;
using System.Threading;
using DroneFlightModule.Authorization;
using DroneFlightModule.Coordination;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;
using DroneFlightModule.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace DroneFlightModule.Composition.Tests
{
    public class InputDeviceStationsTests : FleetTestFixture
    {
        private static readonly PilotId Ace = TestFleetSettings.RegisteredPilot.Id;
        private static readonly PilotId Guest = PilotProfile.Guest.Id;

        private InputDeviceStations stations;

        public override void Setup()
        {
            base.Setup();
            stations = fleet.Resolve<InputDeviceStations>();
        }

        private PilotStation KeyboardStation => stations.Stations[0];

        private void RunBoth(float seconds)
        {
            int ticks = Mathf.RoundToInt(seconds / DeltaTime);
            for (int i = 0; i < ticks; i++)
            {
                airborne.Resolve<FlightLoop>().Tick(DeltaTime);
                grounded.Resolve<FlightLoop>().Tick(DeltaTime);
            }
        }

        private float Height(IObjectResolver drone) => drone.Resolve<FlightLoop>().State.Position.Y;

        [Test]
        public void AtStart_TheKeyboardGetsAStation_AsTheGuest_AndTheMouseNone()
        {
            InputSystem.AddDevice<Mouse>();

            Assert.That(stations.Stations, Has.Count.EqualTo(1));
            Assert.That(KeyboardStation.Name, Is.EqualTo("Keyboard 1"));
            Assert.That(KeyboardStation.Session.PilotId, Is.EqualTo(Guest));
        }

        [Test]
        public void APluggedInPad_GetsItsOwnStation_AsTheNextFreePilot()
        {
            var added = new List<PilotStation>();
            stations.StationAdded += added.Add;

            InputSystem.AddDevice<Gamepad>();

            Assert.That(stations.Stations, Has.Count.EqualTo(2));
            Assert.That(added, Is.EqualTo(new[] { stations.Stations[1] }));
            Assert.That(stations.Stations[1].Name, Is.EqualTo("Gamepad 1"));
            Assert.That(stations.Stations[1].Session.PilotId, Is.EqualTo(Ace));
        }

        [Test]
        public void WhenEveryPilotIsTaken_ANewStationStartsAsTheGuest()
        {
            InputSystem.AddDevice<Gamepad>();
            InputSystem.AddDevice<Gamepad>();

            Assert.That(stations.Stations[2].Name, Is.EqualTo("Gamepad 2"));
            Assert.That(stations.Stations[2].Session.PilotId, Is.EqualTo(Guest));
        }

        [Test]
        public void TheKeyboardAndAPad_FlyTwoDronesAtOnce_EachMovingOnlyItsOwn()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            Tap(keyboard.rKey);
            Tap(pad.buttonSouth);
            AssertFlownBy(grounded, Guest);
            AssertFlownBy(airborne, Ace);

            float start = Height(airborne);
            Set(pad.leftStick, new Vector2(0f, 1f));
            RunBoth(1f);
            Set(pad.leftStick, Vector2.zero);

            Assert.That(Height(airborne), Is.GreaterThan(start + 0.5f), "The pad climbs its own drone.");
            Assert.That(grounded.Resolve<FlightLoop>().State.IsGrounded, Is.True, "The pad never reaches the keyboard's drone.");

            Press(keyboard.spaceKey);
            RunBoth(1f);
            Release(keyboard.spaceKey);

            Assert.That(Height(grounded), Is.GreaterThan(GroundHeight + 0.5f), "The keyboard climbs its own drone.");
        }

        [Test]
        public void TwoPads_StayIndependent()
        {
            var first = InputSystem.AddDevice<Gamepad>();
            var second = InputSystem.AddDevice<Gamepad>();
            Tap(first.buttonSouth);
            Tap(second.buttonSouth);
            AssertFlownBy(grounded, Ace);
            AssertFlownBy(airborne, Guest);

            Set(first.leftStick, new Vector2(0f, 1f));
            RunBoth(1f);

            Assert.That(Height(grounded), Is.GreaterThan(GroundHeight + 0.5f), "The first pad climbs its drone.");
            Assert.That(Height(airborne), Is.EqualTo(5f).Within(0.2f), "The second pad's drone hovers, hands off.");
        }

        [Test]
        public void UnpluggingAPad_HoldsAndLandsItsDrone_AndPluggingItBackResumes()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            Tap(pad.dpad.left);
            var padStation = stations.Stations[1];
            AssertFlownBy(airborne, Ace);

            InputSystem.RemoveDevice(pad);
            Assert.That(padStation.Link.IsConnected, Is.False);
            Assert.That(airborne.Resolve<IFlightGate>().IsFlightAllowed, Is.False);
            RunBoth(5f);
            Assert.That(airborne.Resolve<FlightLoop>().Mode, Is.EqualTo(FlightMode.Holding));

            InputSystem.AddDevice(pad);
            RunBoth(DeltaTime);

            Assert.That(stations.Stations, Has.Count.EqualTo(2), "The pad comes back to its station.");
            AssertFlownBy(airborne, Ace);
            Assert.That(airborne.Resolve<FlightLoop>().Mode, Is.EqualTo(FlightMode.Piloted));

            InputSystem.RemoveDevice(pad);
            RunBoth(30f);
            Assert.That(airborne.Resolve<FlightLoop>().State.IsGrounded, Is.True);
        }

        [Test]
        public void WhileAPadIsUnplugged_AnotherPadCannotStandInForIt()
        {
            var first = InputSystem.AddDevice<Gamepad>();
            var second = InputSystem.AddDevice<Gamepad>();
            Tap(first.dpad.left);
            var firstStation = stations.Stations[1];

            InputSystem.RemoveDevice(first);
            Set(second.leftStick, new Vector2(0f, 1f));
            Tap(second.dpad.left);

            Assert.That(firstStation.Link.IsConnected, Is.False);
            Assert.That(airborne.Resolve<InputRouter>().ActivePilotId, Is.EqualTo(Ace), "The second pad's press reached only its own station.");
            Assert.That(airborne.Resolve<IFlightGate>().IsFlightAllowed, Is.False);
        }

        [Test]
        public void TheSamePilotOnTwoDevices_MovesTheirOneDroneToTheLatestRequest()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            Tap(keyboard.rKey);
            var padStation = stations.Stations[1];
            padStation.SelectPilot(Guest);

            Tap(pad.buttonSouth);
            Set(pad.leftStick, new Vector2(0f, 1f));
            RunBoth(1f);

            AssertFlownBy(grounded, Guest);
            Assert.That(Height(grounded), Is.GreaterThan(GroundHeight + 0.5f), "The pad now steers the guest's drone.");
            Assert.That(airborne.Resolve<InputRouter>().ActivePilotId.IsEmpty, Is.True, "The guest still flies one drone.");
            Assert.That(padStation.TryGetDrone(out _), Is.True);
            Assert.That(KeyboardStation.TryGetDrone(out _), Is.False, "Only the station that steers says it flies.");
            Assert.That(KeyboardStation.Describe(), Does.Contain("no drone"));

            Tap(keyboard.xKey);

            Assert.That(KeyboardStation.LastMessage, Is.EqualTo("Not flying a drone."));
            AssertFlownBy(grounded, Guest);
        }

        [Test]
        public void APadPilot_TakesOverTheKeyboardPilotsDrone_ByPriority()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            Tap(keyboard.rKey);

            Tap(pad.dpad.up);

            AssertFlownBy(grounded, Ace);
            Assert.That(stations.Stations[1].LastMessage, Is.EqualTo("Took over drone-2 from guest."));
            Assert.That(KeyboardStation.LastMessage, Is.EqualTo("ace took drone-2 over from guest."));
            Assert.That(KeyboardStation.TryGetDrone(out _), Is.False);
        }

        [Test]
        public void AParkedPilot_HearsThatTheirDroneWasTaken()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            Tap(keyboard.rKey);
            Tap(keyboard.pKey);

            Tap(pad.dpad.up);

            Assert.That(KeyboardStation.Session.PilotId, Is.EqualTo(Ace));
            Assert.That(KeyboardStation.LastMessage, Is.EqualTo("ace took drone-2 over from guest."));
        }

        [Test]
        public void AnUnpluggedPad_CannotTakeADrone()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            Tap(keyboard.digit1Key);
            var console = fleet.Resolve<IFleetConsole>();
            console.SelectStation("Gamepad 1");
            InputSystem.RemoveDevice(pad);

            console.RequestFlight(AirborneDrone);

            AssertFlownBy(airborne, Guest);
            Assert.That(console.LastMessage, Is.EqualTo("ace is not connected."));
        }

        [Test]
        public void ADroneLandedByAnUnpluggedPad_IsFreedForOthers()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            Tap(pad.dpad.up);
            AssertFlownBy(grounded, Ace);

            InputSystem.RemoveDevice(pad);
            new UnattendedDroneRunner(fleet.Resolve<FlightCoordinator>()).FixedTick();
            AssertFree(grounded);

            Tap(keyboard.digit2Key);
            AssertFlownBy(grounded, Guest);
        }

        [Test]
        public void ANewStation_StartsAsTheLowestRankedFreePilot()
        {
            var john = new PilotProfile(new PilotId("john"), "John", 60);
            var withJohn = new InputDeviceStations(
                new[] { new InputDeviceGroup("Keyboard", "Keyboard"), new InputDeviceGroup("Gamepad", "Gamepad") },
                controls,
                fleet.Resolve<FlightCoordinator>(),
                fleet.Resolve<DroneRegistry>(),
                new InMemoryPilotRegistry(new[] { john, TestFleetSettings.RegisteredPilot }),
                fleet.Resolve<LocalGuestAuthorizationProvider>(),
                fleet.Resolve<IRevokingAuthorizationProvider>(),
                null,
                CancellationToken.None);

            InputSystem.AddDevice<Gamepad>();
            InputSystem.AddDevice<Gamepad>();

            Assert.That(withJohn.Stations[0].Session.PilotId, Is.EqualTo(Guest));
            Assert.That(withJohn.Stations[1].Session.PilotId, Is.EqualTo(Ace));
            Assert.That(withJohn.Stations[2].Session.PilotId, Is.EqualTo(john.Id));
            withJohn.Dispose();
        }

        [Test]
        public void Describe_GivesThePilotDroneAndLink()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            var padStation = stations.Stations[1];
            Assert.That(KeyboardStation.Describe(), Is.EqualTo("Keyboard 1: guest, no drone, connected"));

            Tap(pad.dpad.left);
            Assert.That(padStation.Describe(), Is.EqualTo("Gamepad 1: ace, flying drone-1, connected"));

            Tap(pad.selectButton);
            Assert.That(padStation.Describe(), Is.EqualTo("Gamepad 1: ace, flying drone-1, link cut"));

            Tap(pad.selectButton);
            InputSystem.RemoveDevice(pad);
            Assert.That(padStation.Describe(), Is.EqualTo("Gamepad 1: ace, flying drone-1, disconnected"));
        }

        [Test]
        public void TheConsole_SaysWhenTheChosenStationChanges()
        {
            InputSystem.AddDevice<Gamepad>();
            var console = fleet.Resolve<FleetConsole>();
            int changes = 0;
            console.Changed += () => changes++;

            stations.Stations[1].ToggleLink();
            Assert.That(changes, Is.EqualTo(0), "Another station's change is not the console's.");

            console.SelectStation("Gamepad 1");
            Assert.That(changes, Is.EqualTo(1));

            stations.Stations[1].ToggleLink();
            Assert.That(changes, Is.EqualTo(2));
        }

        [Test]
        public void WithNoStation_TheConsoleSaysSo_AndItsActionsDoNothing()
        {
            var builder = new ContainerBuilder();
            new FleetInstaller(
                new TestFleetSettings().Create(GroundHeight, TimeSpan.Zero, SpawnStep, Array.Empty<InputDeviceGroup>()),
                controls,
                CancellationToken.None,
                null,
                null).Install(builder);
            var empty = builder.Build();
            var console = empty.Resolve<IFleetConsole>();

            Assert.That(console.Stations, Is.Empty);
            Assert.That(console.LastMessage, Is.EqualTo("No input device is connected."));
            Assert.That(console.TryGetDrone(out _), Is.False);
            Assert.DoesNotThrow(() =>
            {
                console.RequestFlight(null);
                console.Release();
                console.ToggleLink();
            });
            empty.Dispose();
        }

        [Test]
        public void TheCamera_FollowsTheChosenStationsDrone()
        {
            var prefab = new GameObject("Drone Prefab", typeof(DroneView)).GetComponent<DroneView>();
            var camera = new GameObject("Camera", typeof(FollowCamera)).GetComponent<FollowCamera>();
            var builder = new ContainerBuilder();
            new FleetInstaller(settings, controls, CancellationToken.None, prefab, null).Install(builder);
            var viewed = builder.Build();
            var instantiator = viewed.Resolve<DroneInstantiator>();
            var first = instantiator.AddDrone();
            var second = instantiator.AddDrone();
            var console = viewed.Resolve<FleetConsole>();
            var follower = new StationFollowCamera(console, instantiator, camera);
            follower.Follow(first);
            instantiator.TryGetView(first, out var firstView);
            instantiator.TryGetView(second, out var secondView);
            Assert.That(camera.Target, Is.EqualTo(firstView.transform));

            console.RequestFlight(second);
            Assert.That(camera.Target, Is.EqualTo(secondView.transform));

            follower.Dispose();
            console.Release();
            console.RequestFlight(first);
            Assert.That(camera.Target, Is.EqualTo(secondView.transform), "Disposing stops following.");

            viewed.Dispose();
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
            UnityEngine.Object.DestroyImmediate(prefab.gameObject);
        }

        [Test]
        public void EachStation_CutsOnlyItsOwnLink()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            Tap(keyboard.rKey);
            Tap(pad.buttonSouth);

            Tap(pad.selectButton);

            Assert.That(stations.Stations[1].Link.IsLinkCut, Is.True);
            Assert.That(airborne.Resolve<IFlightGate>().IsFlightAllowed, Is.False);
            AssertFlownBy(grounded, Guest);
        }

        [Test]
        public void TheConsole_ChoosesAStation_AndActsForIt()
        {
            InputSystem.AddDevice<Gamepad>();
            var console = fleet.Resolve<IFleetConsole>();
            Assert.That(console.Stations, Is.EqualTo(new[] { "Keyboard 1", "Gamepad 1" }));
            Assert.That(console.Station, Is.EqualTo("Keyboard 1"));

            console.SelectStation("Gamepad 1");
            console.RequestFlight(AirborneDrone);

            Assert.That(console.Pilot, Is.EqualTo(Ace));
            AssertFlownBy(airborne, Ace);
            Assert.That(stations.Stations[1].TryGetDrone(out _), Is.True);
            Assert.That(KeyboardStation.TryGetDrone(out _), Is.False);
            Assert.Throws<ArgumentException>(() => console.SelectStation("Joystick 1"));
        }

        [Test]
        public void OnlyConfiguredDevices_GetStations()
        {
            var padsOnly = Stations(new InputDeviceGroup("Gamepad", "Gamepad"));
            Assert.That(padsOnly.Stations, Is.Empty, "The keyboard is not configured.");

            InputSystem.AddDevice<Gamepad>();

            Assert.That(padsOnly.Stations, Has.Count.EqualTo(1));
            Assert.That(padsOnly.Stations[0].Name, Is.EqualTo("Gamepad 1"));
            Assert.That(padsOnly.Stations[0].Session.PilotId, Is.EqualTo(Guest), "The first station starts as the guest.");
            padsOnly.Dispose();
        }

        [Test]
        public void ADeviceGroup_MustNameAKnownLayoutAndAControlScheme()
        {
            Assert.Throws<ArgumentException>(() => Stations(new InputDeviceGroup("Steering Wheel", "Gamepad")));
            Assert.Throws<ArgumentException>(() => Stations(new InputDeviceGroup("Gamepad", "Joystick")));
        }

        private InputDeviceStations Stations(InputDeviceGroup group)
        {
            return new InputDeviceStations(
                new[] { group },
                controls,
                fleet.Resolve<FlightCoordinator>(),
                fleet.Resolve<DroneRegistry>(),
                fleet.Resolve<IPilotRegistry>(),
                fleet.Resolve<LocalGuestAuthorizationProvider>(),
                fleet.Resolve<IRevokingAuthorizationProvider>(),
                null,
                CancellationToken.None);
        }

        [Test]
        public void Disposing_EndsEveryStation_AndIgnoresNewDevices()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            var padStation = stations.Stations[1];

            fleet.Dispose();
            fleet = null;
            InputSystem.AddDevice<Gamepad>();

            Assert.That(stations.Stations, Is.Empty);
            Assert.That(padStation.Link.IsConnected, Is.False);
            Assert.DoesNotThrow(() => Tap(pad.buttonSouth));
        }
    }
}
