using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DroneFlightModule.UI.Tests
{
    public class FleetDashboardTests : InputTestFixture
    {
        private const string ControlsPath = "Assets/DroneFlightModule/Unity/Input/DroneControls.inputactions";

        private Keyboard keyboard;
        private InputActionAsset controls;
        private FleetDashboardReadout readout;
        private FleetDashboard dashboard;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            controls = InputActionAsset.FromJson(File.ReadAllText(ControlsPath));
            var fleet = new TestFleet();
            var log = new FleetEventLog(fleet.Coordinator, fleet.Clock, capacity: 10);
            readout = new FleetDashboardReadout(
                fleet.Drones, fleet.Coordinator, fleet.Pilots, fleet.Stats, fleet.Telemetry, fleet.Terrain, log);
            dashboard = new GameObject("Dashboard", typeof(FleetDashboard)).GetComponent<FleetDashboard>();
        }

        public override void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(dashboard.gameObject);
            UnityEngine.Object.DestroyImmediate(controls);
            base.TearDown();
        }

        private InputAction Toggle => controls.FindAction("Debug/ToggleDashboard", throwIfNotFound: true);

        [Test]
        public void Bind_RequiresTheReadoutAndTheToggle()
        {
            Assert.Throws<ArgumentNullException>(() => dashboard.Bind(null, Toggle));
            Assert.Throws<ArgumentNullException>(() => dashboard.Bind(readout, null));
        }

        [Test]
        public void TheToggle_IsBoundToTab_AndEnabledByBinding()
        {
            dashboard.Bind(readout, Toggle);

            Assert.That(Toggle.enabled, Is.True);
            Assert.That(Toggle.bindings[0].path, Is.EqualTo("<Keyboard>/tab"));
        }

        [Test]
        public void Tab_HidesAndShowsTheDashboard()
        {
            dashboard.Bind(readout, Toggle);
            Assert.That(dashboard.IsVisible, Is.True);

            Press(keyboard.tabKey);
            Release(keyboard.tabKey);
            Assert.That(dashboard.IsVisible, Is.False);

            Press(keyboard.tabKey);
            Release(keyboard.tabKey);
            Assert.That(dashboard.IsVisible, Is.True);
        }
    }
}
