using System;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;
using DroneFlightModule.Telemetry;
using UnityEngine;

namespace DroneFlightModule.Composition.Tests
{
    /// <summary>
    /// Fleet settings for tests: the Core defaults, one registered pilot above the guest, and no
    /// starting drones, with the ground, server latency and spawn step each test needs.
    /// </summary>
    internal sealed class TestFleetSettings
    {
        public static readonly PilotProfile RegisteredPilot = new PilotProfile(new PilotId("ace"), "Ace", 50);

        public FleetSettings Create(float groundHeight, TimeSpan serverLatency, Vector3 spawnStep)
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
                Array.Empty<FleetDroneEntry>());
        }
    }
}
