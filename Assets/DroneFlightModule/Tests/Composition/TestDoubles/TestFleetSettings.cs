using System;
using System.Collections.Generic;
using System.IO;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;
using DroneFlightModule.Telemetry;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DroneFlightModule.Composition.Tests
{
    /// <summary>
    /// The controls and fleet settings for tests: the Core defaults, one registered pilot above the guest, and no
    /// starting drones, with the ground, server latency and spawn step each test needs. Stations
    /// go to the keyboard and gamepads unless a test names other device groups.
    /// </summary>
    internal sealed class TestFleetSettings
    {
        public const float SilenceTimeout = 15f;
        public const string ControlsPath = "Assets/DroneFlightModule/Unity/Input/DroneControls.inputactions";

        public static readonly PilotProfile RegisteredPilot = new PilotProfile(new PilotId("ace"), "Ace", 50);

        public InputActionAsset LoadControls()
        {
            return InputActionAsset.FromJson(File.ReadAllText(ControlsPath));
        }

        public FleetSettings Create(float groundHeight, TimeSpan serverLatency, Vector3 spawnStep)
        {
            return Create(groundHeight, serverLatency, spawnStep, new[]
            {
                new InputDeviceGroup("Keyboard", "Keyboard"),
                new InputDeviceGroup("Gamepad", "Gamepad", SilenceTimeout),
            });
        }

        public FleetSettings Create(float groundHeight, TimeSpan serverLatency, Vector3 spawnStep, IReadOnlyList<InputDeviceGroup> deviceGroups)
        {
            return new FleetSettings(
                DroneConfig.Default,
                VelocityControllerConfig.Default,
                RangefinderConfig.Default,
                FailsafeConfig.Default,
                TelemetryConfig.Default,
                new[] { RegisteredPilot },
                serverLatency,
                groundHeight,
                spawnStep,
                Array.Empty<FleetDroneEntry>(),
                deviceGroups);
        }
    }
}
