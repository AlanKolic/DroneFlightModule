using System;
using System.Threading;
using DroneFlightModule.Core;
using DroneFlightModule.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Composition root of the fleet scene. Installs the fleet with the settings its
    /// <see cref="FleetSettingsAsset"/> maps to, then has the <see cref="DroneInstantiator"/> build
    /// the layout's drones from the prefab. The local pilot starts as the guest without a drone;
    /// the camera starts on the first drone and then follows the drone they fly. The scope flies with its
    /// own copy of the controls asset, and its lifetime token, which pending requests use, is
    /// cancelled when it is destroyed or play mode exits.
    /// </summary>
    public sealed class FleetLifetimeScope : LifetimeScope
    {
        [SerializeField] private FleetSettingsAsset settings;
        [SerializeField] private InputActionAsset controls;
        [SerializeField] private DroneView dronePrefab;
        [SerializeField] private FlightDebugHud debugHud;
        [SerializeField] private FollowCamera followCamera;
        [SerializeField] private Transform groundPlane;

        private InputActionAsset ownControls;
        private CancellationTokenSource lifetime;
        private LocalPilotControls localPilot;
        private Transform followed;

        public DroneInstantiator Instantiator { get; private set; }

        protected override void Configure(IContainerBuilder builder)
        {
            if (settings == null)
            {
                throw new InvalidOperationException($"{name} has no fleet settings asset assigned.");
            }
            if (controls == null)
            {
                throw new InvalidOperationException($"{name} has no controls asset assigned.");
            }
            var fleetSettings = settings.ToSettings();
            ownControls = Instantiate(controls);
            lifetime = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken, Application.exitCancellationToken);
            var fleet = new FleetInstaller(fleetSettings, ownControls, lifetime.Token, dronePrefab, debugHud);
            fleet.Install(builder);
            builder.RegisterEntryPoint<SimulationClockRunner>();

            builder.RegisterBuildCallback(container =>
            {
                PlaceGround(fleetSettings.GroundHeight);
                if (debugHud != null)
                {
                    debugHud.Bind(ownControls);
                }

                Instantiator = container.Resolve<DroneInstantiator>();
                foreach (var entry in fleetSettings.Drones)
                {
                    Instantiator.AddDrone(new DroneId(entry.Id), entry.SpawnPosition);
                }
                if (fleetSettings.Drones.Count > 0 && Instantiator.TryGetView(new DroneId(fleetSettings.Drones[0].Id), out var first))
                {
                    Follow(first.transform);
                }

                fleet.Start(container);
                localPilot = container.Resolve<LocalPilotControls>();
                localPilot.StatusChanged += OnLocalPilotChanged;
            });
        }

        protected override void OnDestroy()
        {
            // Unhooked first: disposing the fleet releases drones, and the views may already be gone.
            if (localPilot != null)
            {
                localPilot.StatusChanged -= OnLocalPilotChanged;
            }
            lifetime?.Cancel();
            base.OnDestroy();
            lifetime?.Dispose();
            if (ownControls != null)
            {
                Destroy(ownControls);
                ownControls = null;
            }
        }

        private void PlaceGround(float groundHeight)
        {
            if (groundPlane == null)
            {
                return;
            }
            var position = groundPlane.position;
            groundPlane.position = new Vector3(position.x, groundHeight, position.z);
        }

        private void OnLocalPilotChanged()
        {
            // Compared by transform: a removed drone's id can come back on a new drone.
            if (localPilot.TryGetDrone(out var drone) && Instantiator.TryGetView(drone, out var view) && view.transform != followed)
            {
                Follow(view.transform);
            }
        }

        private void Follow(Transform target)
        {
            if (followCamera == null)
            {
                return;
            }
            followed = target;
            followCamera.SetTarget(target);
        }
    }
}
