using System;
using System.IO;
using System.Threading;
using DroneFlightModule.Authorization;
using DroneFlightModule.Composition;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace DroneFlightModule.UI.Tests
{
    public class FlightDebugHudTests : InputTestFixture
    {
        private const string ControlsPath = "Assets/DroneFlightModule/Unity/Input/DroneControls.inputactions";

        private sealed class NoProviders : IAuthorizationProviderSelector
        {
            public IAuthorizationProvider For(PilotProfile pilot) => null;
        }

        private FlightDebugHud hud;
        private InputActionAsset controls;
        private PilotStation station;

        public override void Setup()
        {
            base.Setup();
            hud = new GameObject("HUD", typeof(FlightDebugHud)).GetComponent<FlightDebugHud>();
            controls = InputActionAsset.FromJson(File.ReadAllText(ControlsPath));
            var fleet = new TestFleet();
            station = new PilotStation(
                "Keyboard",
                InputSystem.AddDevice<Keyboard>(),
                Object.Instantiate(controls),
                0f,
                new PatrolAutopilot(fleet.Clock),
                fleet.Coordinator,
                new PilotSeating(fleet.Coordinator, fleet.Pilots, new NoProviders()),
                CancellationToken.None);
        }

        public override void TearDown()
        {
            station.Dispose();
            Object.DestroyImmediate(hud.gameObject);
            Object.DestroyImmediate(controls);
            base.TearDown();
        }

        [Test]
        public void Bind_DescribesTheControlsFromTheirBindings()
        {
            hud.Bind(controls);

            Assert.That(hud.ControlsText, Does.Contain("request any drone"));
            Assert.That(hud.ControlsText, Does.Contain("remove the newest drone"));
            Assert.That(hud.ControlsText.Split('\n'), Has.Length.EqualTo(15));
        }

        [Test]
        public void AStation_IsListedWithItsDescription()
        {
            hud.AddStation(station);

            Assert.That(hud.StationTexts, Is.EqualTo(new[] { "Keyboard: empty, connected" }));
        }

        [Test]
        public void ARemovedStation_LeavesTheList()
        {
            hud.AddStation(station);

            hud.RemoveStation(station);

            Assert.That(hud.StationTexts, Is.Empty);
        }

        [Test]
        public void Arguments_AreRequired()
        {
            Assert.Throws<ArgumentNullException>(() => hud.Bind(null));
            Assert.Throws<ArgumentNullException>(() => hud.AddStation(null));
        }
    }
}
