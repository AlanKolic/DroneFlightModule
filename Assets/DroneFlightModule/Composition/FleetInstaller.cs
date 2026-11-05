using System;
using System.Collections.Generic;
using System.Threading;
using DroneFlightModule.Authorization;
using DroneFlightModule.Coordination;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;
using DroneFlightModule.Telemetry;
using DroneFlightModule.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Registers what the whole fleet shares: frame and failsafe configs, the ground, pilots and
    /// their stats, both authorization providers, the coordinator, the simulation clock, the
    /// telemetry config and the fleet-wide in-memory telemetry sink, the pilot at this machine and
    /// their keys, the console a control panel drives, and the instantiator that adds and removes
    /// drones. The fleet starts empty: each drone's child scope adds its own objects with
    /// <see cref="DroneInstaller"/> and joins it, at startup or later. Kept apart from the scope
    /// so tests can build the fleet without a scene; they pass no prefab or HUD.
    /// </summary>
    public sealed class FleetInstaller : IInstaller
    {
        public static readonly PilotProfile RegisteredPilot = new PilotProfile(new PilotId("ace"), "Ace", 50);

        private readonly float groundHeight;
        private readonly InputActionAsset controls;
        private readonly TimeSpan serverLatency;
        private readonly CancellationToken lifetime;
        private readonly DroneView dronePrefab;
        private readonly FlightDebugHud hud;
        private readonly Vector3 spawnStep;

        public FleetInstaller(
            float groundHeight,
            InputActionAsset controls,
            TimeSpan serverLatency,
            CancellationToken lifetime,
            DroneView dronePrefab,
            FlightDebugHud hud,
            Vector3 spawnStep)
        {
            this.groundHeight = groundHeight;
            this.controls = controls;
            this.serverLatency = serverLatency;
            this.lifetime = lifetime;
            this.dronePrefab = dronePrefab;
            this.hud = hud;
            this.spawnStep = spawnStep;
        }

        public void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(DroneConfig.Default);
            builder.RegisterInstance(FailsafeConfig.Default);
            builder.RegisterInstance(VelocityControllerConfig.Default);
            builder.RegisterInstance<ITerrain>(new FlatTerrain(groundHeight));

            builder.Register<IPilotRegistry, InMemoryPilotRegistry>(Lifetime.Singleton)
                .WithParameter<IEnumerable<PilotProfile>>(new[] { RegisteredPilot });
            builder.Register<IPilotStatsStore, InMemoryPilotStatsStore>(Lifetime.Singleton);
            builder.Register<PilotStatsRecorder>(Lifetime.Singleton);

            builder.Register<LocalGuestAuthorizationProvider>(Lifetime.Singleton)
                .WithParameter("guestPilot", PilotProfile.Guest.Id);
            builder.Register<IDelay, TaskDelay>(Lifetime.Singleton);
            builder.Register<IRevokingAuthorizationProvider, SimulatedServerAuthorizationProvider>(Lifetime.Singleton)
                .AsSelf()
                .WithParameter<IEnumerable<PilotId>>(new[] { RegisteredPilot.Id })
                .WithParameter("latency", serverLatency);

            builder.Register<DroneRegistry>(Lifetime.Singleton)
                .WithParameter<IEnumerable<FleetDrone>>(Array.Empty<FleetDrone>());
            builder.Register<IAssignmentPolicy, PriorityAssignmentPolicy>(Lifetime.Singleton);
            builder.Register<FlightCoordinator>(Lifetime.Singleton);
            builder.Register<SimulationClock>(Lifetime.Singleton).AsSelf().As<ISimulationClock>();
            builder.Register<PilotStatsListener>(Lifetime.Singleton);

            builder.RegisterInstance(TelemetryConfig.Default);
            builder.Register<InMemoryTelemetrySink>(Lifetime.Singleton);

            builder.RegisterInstance(controls);
            builder.Register<InputActionInputProvider>(Lifetime.Singleton);
            builder.Register<DisconnectableInputProvider>(Lifetime.Singleton)
                .WithParameter<IInputProvider>(resolver => resolver.Resolve<InputActionInputProvider>());
            builder.Register<LocalPilotControls>(Lifetime.Singleton)
                .WithParameter("lifetime", lifetime);

            builder.Register<DroneInstantiator>(Lifetime.Singleton)
                .WithParameter("prefab", dronePrefab)
                .WithParameter("hud", hud)
                .WithParameter("groundHeight", groundHeight)
                .WithParameter("spawnStep", spawnStep);
            builder.Register<IFleetConsole, FleetConsole>(Lifetime.Singleton);
            builder.Register<FleetKeys>(Lifetime.Singleton);
        }

        public void Start(IObjectResolver fleet)
        {
            // Nothing depends on these, so they start by being resolved. Drones can join before or after.
            if (fleet == null)
            {
                throw new ArgumentNullException(nameof(fleet));
            }
            fleet.Resolve<FlightCoordinator>().ListenerFailed += Debug.LogException;
            fleet.Resolve<PilotStatsListener>();
            fleet.Resolve<FleetKeys>();
        }
    }
}
