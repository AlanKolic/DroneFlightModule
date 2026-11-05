using System;
using UnityEngine;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// The one asset the fleet scope reads: it points at the asset for each part of the fleet's
    /// configuration and maps them all to a <see cref="FleetSettings"/>. A missing part fails with
    /// its name, so the scene never falls back to defaults it wasn't given.
    /// </summary>
    [CreateAssetMenu(menuName = "DroneFlightModule/Fleet Settings", fileName = "FleetSettings")]
    public sealed class FleetSettingsAsset : ScriptableObject
    {
        [SerializeField] private DroneFrameAsset frame;
        [SerializeField] private VelocityControllerAsset velocityController;
        [SerializeField] private RangefinderAsset rangefinder;
        [SerializeField] private FailsafeAsset failsafe;
        [SerializeField] private TelemetryAsset telemetry;
        [SerializeField] private PilotRosterAsset pilots;
        [SerializeField] private SimulatedServerAsset server;
        [SerializeField] private FleetLayoutAsset layout;

        public FleetSettings ToSettings()
        {
            var fleetLayout = Require(layout, nameof(layout));
            return new FleetSettings(
                Require(frame, nameof(frame)).ToConfig(),
                Require(velocityController, nameof(velocityController)).ToConfig(),
                Require(rangefinder, nameof(rangefinder)).ToConfig(),
                Require(failsafe, nameof(failsafe)).ToConfig(),
                Require(telemetry, nameof(telemetry)).ToConfig(),
                Require(pilots, nameof(pilots)).ToProfiles(),
                Require(server, nameof(server)).ToLatency(),
                fleetLayout.GroundHeight,
                fleetLayout.SpawnStep,
                fleetLayout.Drones);
        }

        private T Require<T>(T part, string field) where T : ScriptableObject
        {
            if (part == null)
            {
                throw new InvalidOperationException($"{name} has no {field} asset assigned.");
            }
            return part;
        }
    }
}
