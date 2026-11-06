using System;
using System.Collections.Generic;
using DroneFlightModule.Composition;
using DroneFlightModule.Core;
using DroneFlightModule.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DroneFlightModule.UI.Tests
{
    public class ScreenFlightPanelTests : InputTestFixture
    {
        private sealed class ChosenStation : IFleetConsole
        {
            public IReadOnlyList<string> Stations => new[] { Station };
            public string Station { get; set; } = "Keyboard";
            public PilotId Pilot => default;
            public string LastMessage => string.Empty;
            public bool IsLinkCut => false;
            public string LinkStatus => string.Empty;

            public InputDevice Chosen { get; set; }

            public void SelectStation(string station)
            {
                Station = station;
            }

            public string SeatOf(PilotId pilot)
            {
                return string.Empty;
            }

            public bool IsChosen(InputDevice device)
            {
                return device == Chosen;
            }

            public bool TryGetDrone(out DroneId drone)
            {
                drone = default;
                return false;
            }

            public void ToggleArm()
            {
            }

            public void ToggleAutopilot()
            {
            }

            public void SelectPilot(PilotId pilot)
            {
            }

            public void RequestFlight(DroneId? drone)
            {
            }

            public void Release()
            {
            }

            public void RevokeGrant()
            {
            }

            public void ToggleLink()
            {
            }

            public void AddDrone()
            {
            }

            public void RemoveDrone(DroneId drone)
            {
            }
        }

        private ChosenStation console;
        private ScreenFlightPanel panel;

        public override void Setup()
        {
            base.Setup();
            InputSystem.RegisterLayout<ScreenPanelGamepad>();
            console = new ChosenStation();
            panel = new GameObject("Panel", typeof(ScreenFlightPanel)).GetComponent<ScreenFlightPanel>();
            panel.Bind(console);
        }

        public override void TearDown()
        {
            if (panel != null)
            {
                UnityEngine.Object.DestroyImmediate(panel.gameObject);
            }
            base.TearDown();
        }

        [Test]
        public void Bind_RequiresTheConsole_AndAddsAVirtualGamepad()
        {
            Assert.Throws<ArgumentNullException>(() => panel.Bind(null));
            Assert.That(panel.Device.added, Is.True);
            Assert.That(panel.Device, Is.InstanceOf<ScreenPanelGamepad>());
        }

        [Test]
        public void ThePanel_ShowsOnlyWhileItsStationIsChosen()
        {
            Assert.That(panel.IsVisible, Is.False);

            console.Chosen = panel.Device;
            Assert.That(panel.IsVisible, Is.True);

            console.Chosen = null;
            Assert.That(panel.IsVisible, Is.False);
        }

        [Test]
        public void HeldButtons_ReachTheVirtualGamepadsSticks()
        {
            panel.SetSticks(new Vector2(0f, 1f), new Vector2(-1f, 0f));
            InputSystem.Update();

            Assert.That(panel.Device.rightStick.y.ReadValue(), Is.GreaterThan(0.5f));
            Assert.That(panel.Device.leftStick.x.ReadValue(), Is.LessThan(-0.5f));

            panel.SetSticks(Vector2.zero, Vector2.zero);
            InputSystem.Update();

            Assert.That(panel.Device.rightStick.ReadValue(), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void RemoveDevice_TakesTheVirtualGamepadOutOfTheSystem_AndCanRepeat()
        {
            panel.RemoveDevice();

            Assert.That(panel.Device.added, Is.False);
            Assert.DoesNotThrow(() => panel.RemoveDevice());
        }
    }
}
