using System;
using System.Collections.Generic;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;
using DroneFlightModule.Telemetry;
using UnityEngine;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Everything the fleet is configured with, as validated plain objects: the drone frame,
    /// velocity controller, rangefinder, failsafe and telemetry configs, the registered pilots,
    /// the simulated server's latency, the layout, and the input devices that get a pilot station. The scene reads it from a
    /// <see cref="FleetSettingsAsset"/>; tests build it directly.
    /// </summary>
    public sealed class FleetSettings
    {
        public FleetSettings(
            DroneConfig frame,
            VelocityControllerConfig velocityController,
            RangefinderConfig rangefinder,
            FailsafeConfig failsafe,
            TelemetryConfig telemetry,
            IReadOnlyList<PilotProfile> pilots,
            TimeSpan serverLatency,
            float groundHeight,
            Vector3 spawnStep,
            IReadOnlyList<FleetDroneEntry> drones,
            IReadOnlyList<InputDeviceGroup> deviceGroups)
        {
            Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            VelocityController = velocityController ?? throw new ArgumentNullException(nameof(velocityController));
            Rangefinder = rangefinder ?? throw new ArgumentNullException(nameof(rangefinder));
            Failsafe = failsafe ?? throw new ArgumentNullException(nameof(failsafe));
            Telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
            Pilots = pilots ?? throw new ArgumentNullException(nameof(pilots));
            Drones = drones ?? throw new ArgumentNullException(nameof(drones));
            DeviceGroups = deviceGroups ?? throw new ArgumentNullException(nameof(deviceGroups));
            if (serverLatency < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(serverLatency), serverLatency, "Must not be negative.");
            }
            if (!float.IsFinite(groundHeight))
            {
                throw new ArgumentOutOfRangeException(nameof(groundHeight), groundHeight, "Must be finite.");
            }
            ServerLatency = serverLatency;
            GroundHeight = groundHeight;
            SpawnStep = spawnStep;
        }

        public DroneConfig Frame { get; }
        public VelocityControllerConfig VelocityController { get; }
        public RangefinderConfig Rangefinder { get; }
        public FailsafeConfig Failsafe { get; }
        public TelemetryConfig Telemetry { get; }
        public IReadOnlyList<PilotProfile> Pilots { get; }
        public TimeSpan ServerLatency { get; }
        public float GroundHeight { get; }
        public Vector3 SpawnStep { get; }
        public IReadOnlyList<FleetDroneEntry> Drones { get; }
        public IReadOnlyList<InputDeviceGroup> DeviceGroups { get; }
    }
}
