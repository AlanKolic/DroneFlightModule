using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DroneFlightModule.Authorization;
using DroneFlightModule.Coordination;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;
using DroneFlightModule.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
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

        private FleetRunner Runner => new FleetRunner(fleet.Resolve<SimulationClock>(), fleet.Resolve<FlightCoordinator>(), stations);

        private Gamepad AddPad(string serial)
        {
            return (Gamepad)InputSystem.AddDevice(new InputDeviceDescription { deviceClass = "Gamepad", serial = serial });
        }

        private PilotStation StationOf(InputDevice device) => stations.Stations.First(station => station.Device == device);

        private void Seat(InputDevice device, PilotId pilot)
        {
            StationOf(device).SelectPilot(pilot);
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
            Assert.That(KeyboardStation.Name, Is.EqualTo("Keyboard"));
            Assert.That(KeyboardStation.Session.PilotId, Is.EqualTo(Guest));
        }

        [Test]
        public void ANewDevice_StartsEmpty_WhileTheGuestIsTaken()
        {
            var pad = InputSystem.AddDevice<Gamepad>();

            Assert.That(StationOf(pad).Session, Is.Null);
            Assert.That(StationOf(pad).Describe(), Is.EqualTo("Gamepad: empty, connected"));
        }

        [Test]
        public void ANewDevice_TakesTheGuest_WhenNobodyHasThem()
        {
            KeyboardStation.SelectPilot(default);

            var pad = InputSystem.AddDevice<Gamepad>();

            Assert.That(StationOf(pad).Session.PilotId, Is.EqualTo(Guest));
        }

        [Test]
        public void ChangingThePilot_ReleasesTheDrone_AndTheNewPilotMustRequest()
        {
            Tap(keyboard.rKey);
            AssertFlownBy(grounded, Guest);

            KeyboardStation.SelectPilot(Ace);

            AssertFree(grounded);
            Assert.That(KeyboardStation.TryGetDrone(out _), Is.False);

            Tap(keyboard.digit2Key);

            AssertFlownBy(grounded, Ace);
            Assert.That(fleet.Resolve<IPilotStatsStore>().Get(Ace).Takeovers, Is.EqualTo(0), "The drone was free.");
        }

        [Test]
        public void MovingDesks_KeepsTheDroneFlying_AndEmptiesTheFirstDesk()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            Tap(keyboard.digit1Key);
            var session = KeyboardStation.Session;

            Seat(pad, Guest);

            AssertFlownBy(airborne, Guest);
            Assert.That(StationOf(pad).Session, Is.SameAs(session));
            Assert.That(KeyboardStation.Session, Is.Null);
            Assert.That(KeyboardStation.LastMessage, Is.EqualTo("guest moved to Gamepad."));

            float start = Height(airborne);
            Set(pad.leftStick, new Vector2(0f, 1f));
            RunBoth(1f);
            Assert.That(Height(airborne), Is.GreaterThan(start + 0.5f), "The pad now steers the drone.");
        }

        [Test]
        public void MovingOntoAnIdlePad_KeepsTheFlightGoing()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            Tap(keyboard.digit1Key);
            currentTime += TestFleetSettings.SilenceTimeout + 1f;

            Seat(pad, Guest);

            AssertFlownBy(airborne, Guest);
        }

        [Test]
        public void SittingDownAtAnIdlePad_LeavesItAbleToRequest()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            currentTime += TestFleetSettings.SilenceTimeout + 1f;

            Seat(pad, Ace);
            fleet.Resolve<IFleetConsole>().SelectStation("Gamepad");
            fleet.Resolve<IFleetConsole>().RequestFlight(AirborneDrone);

            AssertFlownBy(airborne, Ace);
        }

        [Test]
        public void AnEmptyDesk_SaysToPickAPilot_AndASeatedOneToRequest()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            Assert.That(StationOf(pad).LastMessage, Is.EqualTo("Pick a pilot for this desk."));

            Seat(pad, Ace);

            Assert.That(StationOf(pad).LastMessage, Is.EqualTo("ace sits here. Request a drone."));
        }

        [Test]
        public void MovingOntoAnOccupiedDesk_ReleasesThatPilotsDrone_AndKeepsTheMovers()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            Seat(pad, Ace);
            Tap(pad.dpad.left);
            Tap(keyboard.digit2Key);

            Seat(pad, Guest);

            AssertFlownBy(grounded, Guest);
            AssertFree(airborne);
            Assert.That(StationOf(pad).Session.PilotId, Is.EqualTo(Guest));
            Assert.That(KeyboardStation.Session, Is.Null);
        }

        [Test]
        public void SelectingThePilotAlreadyThere_ChangesNothing()
        {
            Tap(keyboard.rKey);
            var session = KeyboardStation.Session;

            KeyboardStation.SelectPilot(Guest);

            Assert.That(KeyboardStation.Session, Is.SameAs(session));
            AssertFlownBy(grounded, Guest);
        }

        [Test]
        public void TheSwitchKey_WalksThePilotsThenEmpty()
        {
            Tap(keyboard.pKey);
            Assert.That(KeyboardStation.Session, Is.Null);

            Tap(keyboard.pKey);
            Assert.That(KeyboardStation.Session.PilotId, Is.EqualTo(Ace));

            Tap(keyboard.pKey);
            Assert.That(KeyboardStation.Session.PilotId, Is.EqualTo(Guest));
        }

        [Test]
        public void ChangingThePilot_CancelsAPendingRequest()
        {
            var slow = SlowServerFleet(out var coordinator);
            var desk = slow.Resolve<InputDeviceStations>().Stations[0];
            desk.SelectPilot(Ace);
            var ace = desk.Session;
            desk.RequestFlightAsync(AirborneDrone);

            desk.SelectPilot(default);

            Assert.That(coordinator.CancelRequest(ace), Is.False, "Nothing is left pending.");
            slow.Dispose();
        }

        [Test]
        public void MovingDesks_CancelsAPendingRequest()
        {
            var slow = SlowServerFleet(out var coordinator);
            var pad = InputSystem.AddDevice<Gamepad>();
            var desks = slow.Resolve<InputDeviceStations>();
            desks.Stations[0].SelectPilot(Ace);
            var ace = desks.Stations[0].Session;
            desks.Stations[0].RequestFlightAsync(AirborneDrone);

            desks.Stations[1].SelectPilot(Ace);

            Assert.That(coordinator.CancelRequest(ace), Is.False, "Nothing is left pending.");
            Assert.That(desks.Stations[1].LastMessage, Does.Contain("request was cancelled"));
            slow.Dispose();
        }

        private IObjectResolver SlowServerFleet(out FlightCoordinator coordinator)
        {
            var builder = new ContainerBuilder();
            new FleetInstaller(Settings(TimeSpan.FromMinutes(1)), controls, CancellationToken.None, null).Install(builder);
            var slow = builder.Build();
            slow.Resolve<DroneInstantiator>().AddDrone(AirborneDrone, new Vector3(0f, 5f, 0f));
            coordinator = slow.Resolve<FlightCoordinator>();
            return slow;
        }

        [Test]
        public void APreemptedPilot_StaysSeated_AndCanRequestAgain()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            Seat(pad, Ace);
            Tap(keyboard.rKey);
            Tap(pad.dpad.up);

            Assert.That(KeyboardStation.Session.PilotId, Is.EqualTo(Guest));

            Tap(keyboard.digit1Key);
            AssertFlownBy(airborne, Guest);
        }

        [Test]
        public void AGoneDeviceWithNoDrone_FreesItsPilot()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            Seat(pad, Ace);
            InputSystem.RemoveDevice(pad);
            Runner.FixedTick();

            KeyboardStation.SelectPilot(Ace);

            Assert.That(KeyboardStation.Session.PilotId, Is.EqualTo(Ace));
            Assert.That(KeyboardStation.Link.IsConnected, Is.True);
        }

        [Test]
        public void AnEmptyDesk_ReportsInsteadOfFlying()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            var desk = StationOf(pad);

            foreach (var button in new ButtonControl[] { pad.buttonSouth, pad.dpad.left, pad.buttonEast, pad.buttonNorth })
            {
                Tap(button);
                Assert.That(desk.LastMessage, Is.EqualTo("Nobody is at this desk."));
            }
            Assert.That(desk.TryGetDrone(out _), Is.False);

            Tap(pad.selectButton);
            Assert.That(desk.Link.IsLinkCut, Is.True);
        }

        [Test]
        public void TheKeyboardAndAPad_FlyTwoDronesAtOnce_EachMovingOnlyItsOwn()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            Seat(pad, Ace);
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
            Seat(first, Ace);
            Tap(first.buttonSouth);
            Seat(second, Guest);
            Tap(second.buttonSouth);
            AssertFlownBy(grounded, Ace);
            AssertFlownBy(airborne, Guest);

            Set(first.leftStick, new Vector2(0f, 1f));
            RunBoth(1f);

            Assert.That(Height(grounded), Is.GreaterThan(GroundHeight + 0.5f), "The first pad climbs its drone.");
            Assert.That(Height(airborne), Is.EqualTo(5f).Within(0.2f), "The second pad's drone hovers, untouched.");
        }

        [Test]
        public void UnpluggingAPad_HoldsAndLandsItsDrone_AndPluggingItBackResumes()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            Seat(pad, Ace);
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
        public void APadThatComesBackAsANewDevice_ResumesItsStation()
        {
            var pad = AddPad("pad-1");
            Seat(pad, Ace);
            Tap(pad.dpad.left);
            var padStation = stations.Stations[1];
            InputSystem.RemoveDevice(pad);
            Assert.That(airborne.Resolve<IFlightGate>().IsFlightAllowed, Is.False);

            var returned = AddPad("pad-1");

            Assert.That(stations.Stations, Has.Count.EqualTo(2), "The new device takes the waiting station.");
            Assert.That(padStation.Device, Is.SameAs(returned));
            AssertFlownBy(airborne, Ace);

            float start = Height(airborne);
            Set(returned.leftStick, new Vector2(0f, 1f));
            RunBoth(1f);
            Assert.That(Height(airborne), Is.GreaterThan(start + 0.5f), "The new device steers the drone.");
        }

        [Test]
        public void APadThatGoesSilent_CountsAsDisconnected_AndAnyInputRestoresIt()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            Seat(pad, Ace);
            Tap(pad.dpad.left);
            AssertFlownBy(airborne, Ace);

            currentTime += TestFleetSettings.SilenceTimeout + 1f;

            Assert.That(airborne.Resolve<IFlightGate>().IsFlightAllowed, Is.False);
            Assert.That(stations.Stations[1].LinkStatus, Is.EqualTo("disconnected"));
            RunBoth(1f);
            Assert.That(airborne.Resolve<FlightLoop>().Mode, Is.EqualTo(FlightMode.Holding));

            Press(pad.buttonWest);
            RunBoth(DeltaTime);

            AssertFlownBy(airborne, Ace);
            Assert.That(airborne.Resolve<FlightLoop>().Mode, Is.EqualTo(FlightMode.Piloted));
        }

        [Test]
        public void APadWithinTheTimeout_StaysConnected()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            Seat(pad, Ace);
            Tap(pad.dpad.left);

            currentTime += TestFleetSettings.SilenceTimeout - 1f;

            AssertFlownBy(airborne, Ace);
        }

        [Test]
        public void ANewPad_GetsTheFullTimeoutBeforeItsFirstInput()
        {
            currentTime += 1000.0;

            InputSystem.AddDevice<Gamepad>();

            Assert.That(stations.Stations[1].Link.IsConnected, Is.True);
        }

        [Test]
        public void AKeyboard_NeverTimesOut()
        {
            Tap(keyboard.rKey);

            currentTime += 1000.0;

            AssertFlownBy(grounded, Guest);
        }

        [Test]
        public void AStaleEntryOfThePad_IsReplaced_WhenThePadConnects()
        {
            var stale = AddPad("pad-1");
            var padStation = stations.Stations[1];

            var live = AddPad("pad-1");

            Assert.That(stale.added, Is.True, "The Input System still lists it, as it does after a lost disconnect.");
            Assert.That(stations.Stations, Has.Count.EqualTo(2), "One physical pad, one station.");
            Assert.That(padStation.Device, Is.SameAs(live));

            Seat(live, Ace);
            Tap(live.dpad.left);
            AssertFlownBy(airborne, Ace);
        }

        [Test]
        public void AStaleEntryPresentAtStart_SharesTheLivePadsStation()
        {
            AddPad("pad-1");
            var live = AddPad("pad-1");

            var fresh = Stations(new InputDeviceGroup("Gamepad", "Gamepad", "Gamepad"));

            Assert.That(fresh.Stations, Has.Count.EqualTo(1));
            Assert.That(fresh.Stations[0].Device, Is.SameAs(live));
            fresh.Dispose();
        }

        [Test]
        public void ADifferentPad_DoesNotTakeAWaitingStation()
        {
            var pad = AddPad("pad-1");
            Seat(pad, Ace);
            Tap(pad.dpad.left);
            InputSystem.RemoveDevice(pad);

            AddPad("pad-2");

            Assert.That(stations.Stations, Has.Count.EqualTo(3));
            Assert.That(airborne.Resolve<IFlightGate>().IsFlightAllowed, Is.False);
        }

        [Test]
        public void AGoneDeviceWithNoDrone_LosesItsStation_AndItsNameAndPilotAreFree()
        {
            var pad = InputSystem.AddDevice<Gamepad>();

            InputSystem.RemoveDevice(pad);
            Runner.FixedTick();

            Assert.That(stations.Stations, Has.Count.EqualTo(1));

            InputSystem.AddDevice<Gamepad>();
            Assert.That(stations.Stations[1].Name, Is.EqualTo("Gamepad"));
        }

        [Test]
        public void AGoneDevice_KeepsItsStationWhileItsDroneIsInTheAir()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            Seat(pad, Ace);
            Tap(pad.dpad.left);

            InputSystem.RemoveDevice(pad);
            Runner.FixedTick();
            Assert.That(stations.Stations, Has.Count.EqualTo(2));

            RunBoth(30f);
            Runner.FixedTick();
            Assert.That(stations.Stations, Has.Count.EqualTo(1), "Its drone landed, so nothing waits for the device.");
            AssertFree(airborne);
        }

        [Test]
        public void TheConsole_EmptiesADesk_AndSaysWhereAPilotSits()
        {
            var console = fleet.Resolve<IFleetConsole>();
            Assert.That(console.SeatOf(Guest), Is.EqualTo("Keyboard"));
            Assert.That(console.SeatOf(Ace), Is.Empty);

            console.SelectPilot(default);

            Assert.That(console.Pilot.IsEmpty, Is.True);
            Assert.That(console.TryGetDrone(out _), Is.False);
            Assert.That(console.SeatOf(Guest), Is.Empty);
        }

        [Test]
        public void TheConsole_FallsBackWhenItsStationIsRemoved()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            var console = fleet.Resolve<IFleetConsole>();
            console.SelectStation("Gamepad");

            InputSystem.RemoveDevice(pad);
            Runner.FixedTick();

            Assert.That(console.Station, Is.EqualTo("Keyboard"));
        }

        [Test]
        public void WhileAPadIsUnplugged_AnotherPadCannotStandInForIt()
        {
            var first = InputSystem.AddDevice<Gamepad>();
            var second = InputSystem.AddDevice<Gamepad>();
            Seat(first, Ace);
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
        public void APadPilot_TakesOverTheKeyboardPilotsDrone_ByPriority()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            Seat(pad, Ace);
            Tap(keyboard.rKey);

            Tap(pad.dpad.up);

            AssertFlownBy(grounded, Ace);
            Assert.That(stations.Stations[1].LastMessage, Is.EqualTo("Took over drone-2 from guest."));
            Assert.That(KeyboardStation.LastMessage, Is.EqualTo("ace took drone-2 over from guest."));
            Assert.That(KeyboardStation.TryGetDrone(out _), Is.False);
            var stats = fleet.Resolve<IPilotStatsStore>();
            Assert.That(stats.Get(Ace).Takeovers, Is.EqualTo(1));
            Assert.That(stats.Get(Guest).TimesPreempted, Is.EqualTo(1));
        }

        [Test]
        public void AnUnpluggedPad_CannotTakeADrone()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            Seat(pad, Ace);
            Tap(keyboard.digit1Key);
            var console = fleet.Resolve<IFleetConsole>();
            console.SelectStation("Gamepad");
            InputSystem.RemoveDevice(pad);

            console.RequestFlight(AirborneDrone);

            AssertFlownBy(airborne, Guest);
            Assert.That(console.LastMessage, Is.EqualTo("ace is not connected."));
        }

        [Test]
        public void ADroneLandedByAnUnpluggedPad_IsFreedForOthers()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            Seat(pad, Ace);
            Tap(pad.dpad.up);
            AssertFlownBy(grounded, Ace);

            InputSystem.RemoveDevice(pad);
            Runner.FixedTick();
            AssertFree(grounded);

            Tap(keyboard.digit2Key);
            AssertFlownBy(grounded, Guest);
        }

        [Test]
        public void Describe_GivesThePilotDroneAndLink()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            Seat(pad, Ace);
            var padStation = stations.Stations[1];
            Assert.That(KeyboardStation.Describe(), Is.EqualTo("Keyboard: guest, no drone, connected"));

            Tap(pad.dpad.left);
            Assert.That(padStation.Describe(), Is.EqualTo("Gamepad: ace, flying drone-1, connected"));

            Tap(pad.selectButton);
            Assert.That(padStation.Describe(), Is.EqualTo("Gamepad: ace, flying drone-1, link cut"));

            Tap(pad.selectButton);
            InputSystem.RemoveDevice(pad);
            Assert.That(padStation.Describe(), Is.EqualTo("Gamepad: ace, flying drone-1, disconnected"));
        }

        [Test]
        public void WithNoStation_TheConsoleSaysSo_AndItsActionsDoNothing()
        {
            var builder = new ContainerBuilder();
            new FleetInstaller(
                new TestFleetSettings().Create(GroundHeight, TimeSpan.Zero, SpawnStep, Array.Empty<InputDeviceGroup>()),
                controls,
                CancellationToken.None,
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
            new FleetInstaller(settings, controls, CancellationToken.None, prefab).Install(builder);
            var viewed = builder.Build();
            var instantiator = viewed.Resolve<DroneInstantiator>();
            var first = instantiator.AddDrone();
            var second = instantiator.AddDrone();
            var console = viewed.Resolve<IFleetConsole>();
            var follower = new StationFollowCamera(console, instantiator, camera);
            follower.Follow(first);
            instantiator.TryGetView(first, out var firstView);
            instantiator.TryGetView(second, out var secondView);

            follower.Tick();
            Assert.That(camera.Target, Is.EqualTo(firstView.transform), "With no drone it stays where it is.");

            console.RequestFlight(second);
            follower.Tick();
            Assert.That(camera.Target, Is.EqualTo(secondView.transform));

            viewed.Dispose();
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
            UnityEngine.Object.DestroyImmediate(prefab.gameObject);
        }

        [Test]
        public void EachStation_CutsOnlyItsOwnLink()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            Seat(pad, Ace);
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
            Assert.That(console.Stations, Is.EqualTo(new[] { "Keyboard", "Gamepad" }));
            Assert.That(console.Station, Is.EqualTo("Keyboard"));

            console.SelectStation("Gamepad");
            console.SelectPilot(Ace);
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
            var padsOnly = Stations(new InputDeviceGroup("Gamepad", "Gamepad", "Gamepad"));
            Assert.That(padsOnly.Stations, Is.Empty, "The keyboard is not configured.");

            InputSystem.AddDevice<Gamepad>();

            Assert.That(padsOnly.Stations, Has.Count.EqualTo(1));
            Assert.That(padsOnly.Stations[0].Name, Is.EqualTo("Gamepad"));
            padsOnly.Dispose();
        }

        [Test]
        public void TheScreenPanel_IsAnInputOfItsOwn_WithNoTimeout()
        {
            InputSystem.RegisterLayout<ScreenPanelGamepad>();
            var groups = Stations(
                new InputDeviceGroup("Screen panel", "ScreenPanelGamepad", "Gamepad"),
                new InputDeviceGroup("Gamepad", "Gamepad", "Gamepad", TestFleetSettings.SilenceTimeout));

            InputSystem.AddDevice<ScreenPanelGamepad>();
            InputSystem.AddDevice<Gamepad>();

            Assert.That(groups.Stations[0].Name, Is.EqualTo("Screen panel"));
            Assert.That(groups.Stations[1].Name, Is.EqualTo("Gamepad"));

            currentTime += 1000.0;
            Assert.That(groups.Stations[0].Link.IsConnected, Is.True, "No silence timeout: the panel can sit idle.");
            Assert.That(groups.Stations[1].Link.IsConnected, Is.False);
            groups.Dispose();
        }

        [Test]
        public void ADeviceGroup_MustNameAKnownLayoutAndAControlScheme()
        {
            Assert.Throws<ArgumentException>(() => Stations(new InputDeviceGroup("Wheel", "Steering Wheel", "Gamepad")));
            Assert.Throws<ArgumentException>(() => Stations(new InputDeviceGroup("Gamepad", "Gamepad", "Joystick")));
            Assert.Throws<ArgumentException>(() => Stations(new InputDeviceGroup("Gamepad", string.Empty, "Gamepad")));
            Assert.Throws<ArgumentException>(() => Stations(new InputDeviceGroup(string.Empty, "Gamepad", "Gamepad")));
        }

        private InputDeviceStations Stations(params InputDeviceGroup[] groups)
        {
            return new InputDeviceStations(groups, controls, fleet.Resolve<PilotStationFactory>(), fleet.Resolve<PilotSeating>());
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
