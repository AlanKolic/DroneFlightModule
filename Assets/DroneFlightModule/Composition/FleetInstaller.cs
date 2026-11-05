using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Registers what the whole fleet shares: the configs from the <see cref="FleetSettings"/>,
    /// the ground, pilots and their stats, both authorization providers, the coordinator, the
    /// simulation clock, the fleet-wide in-memory telemetry sink, the pilot at this machine and
    /// their keys, the console a control panel drives, and the instantiator that adds and removes
    /// drones. Every registered pilot signs in through the simulated server. The fleet starts
    /// empty: each drone's child scope adds its own objects with <see cref="DroneInstaller"/> and
    /// joins it, at startup or later. Kept apart from the scope so tests can build the fleet
    /// without a scene; they pass no prefab or HUD.
    /// </summary>
    public sealed class FleetInstaller : IInstaller
    {
        private readonly FleetSettings settings;
        private readonly InputActionAsset controls;
        private readonly CancellationToken lifetime;
        private readonly DroneView dronePrefab;
        private readonly FlightDebugHud hud;

        public FleetInstaller(
            FleetSettings settings,
            InputActionAsset controls,
            CancellationToken lifetime,
            DroneView dronePrefab,
            FlightDebugHud hud)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.controls = controls;
            this.lifetime = lifetime;
            this.dronePrefab = dronePrefab;
            this.hud = hud;
        }

        public void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(settings.Frame);
            builder.RegisterInstance(settings.Failsafe);
            builder.RegisterInstance(settings.VelocityController);
            builder.RegisterInstance(settings.Rangefinder);
            builder.RegisterInstance<ITerrain>(new FlatTerrain(settings.GroundHeight));

            builder.Register<IPilotRegistry, InMemoryPilotRegistry>(Lifetime.Singleton)
                .WithParameter<IEnumerable<PilotProfile>>(settings.Pilots);
            builder.Register<IPilotStatsStore, InMemoryPilotStatsStore>(Lifetime.Singleton);
            builder.Register<PilotStatsRecorder>(Lifetime.Singleton);

            builder.Register<LocalGuestAuthorizationProvider>(Lifetime.Singleton)
                .WithParameter("guestPilot", PilotProfile.Guest.Id);
            builder.Register<IDelay, TaskDelay>(Lifetime.Singleton);
            builder.Register<IRevokingAuthorizationProvider, SimulatedServerAuthorizationProvider>(Lifetime.Singleton)
                .AsSelf()
                .WithParameter<IEnumerable<PilotId>>(settings.Pilots.Select(pilot => pilot.Id).ToArray())
                .WithParameter("latency", settings.ServerLatency);

            builder.Register<DroneRegistry>(Lifetime.Singleton)
                .WithParameter<IEnumerable<FleetDrone>>(Array.Empty<FleetDrone>());
            builder.Register<IAssignmentPolicy, PriorityAssignmentPolicy>(Lifetime.Singleton);
            builder.Register<FlightCoordinator>(Lifetime.Singleton);
            builder.Register<SimulationClock>(Lifetime.Singleton).AsSelf().As<ISimulationClock>();
            builder.Register<PilotStatsListener>(Lifetime.Singleton);

            builder.RegisterInstance(settings.Telemetry);
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
                .WithParameter("groundHeight", settings.GroundHeight)
                .WithParameter("spawnStep", settings.SpawnStep);
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
