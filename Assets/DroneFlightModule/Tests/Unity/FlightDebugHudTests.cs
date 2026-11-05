using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DroneFlightModule.Unity.Tests
{
    public class FlightDebugHudTests
    {
        private const string ControlsPath = "Assets/DroneFlightModule/Unity/Input/DroneControls.inputactions";

        private sealed class FixedStatus : IStationStatus
        {
            public string Describe() => "Keyboard: guest, no drone, connected";
        }

        private FlightDebugHud hud;
        private InputActionAsset controls;

        [SetUp]
        public void SetUp()
        {
            hud = new GameObject("HUD", typeof(FlightDebugHud)).GetComponent<FlightDebugHud>();
            controls = InputActionAsset.FromJson(File.ReadAllText(ControlsPath));
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(hud.gameObject);
            UnityEngine.Object.DestroyImmediate(controls);
        }

        [Test]
        public void Bind_DescribesTheControlsFromTheirBindings()
        {
            hud.Bind(controls);

            Assert.That(hud.ControlsText, Does.Contain("request any drone"));
            Assert.That(hud.ControlsText, Does.Contain("remove the newest drone"));
            Assert.That(hud.ControlsText.Split('\n'), Has.Length.EqualTo(13));
        }

        [Test]
        public void AStation_IsListedWithItsDescription()
        {
            hud.AddStation(new FixedStatus());

            Assert.That(hud.StationTexts, Is.EqualTo(new[] { "Keyboard: guest, no drone, connected" }));
        }

        [Test]
        public void ARemovedStation_LeavesTheList()
        {
            var station = new FixedStatus();
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
