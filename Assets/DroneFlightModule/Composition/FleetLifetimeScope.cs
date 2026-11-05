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
    /// the layout's drones from the prefab. Each input device gets a pilot station, the keyboard's
    /// starting as the guest without a drone; the camera starts on the first drone and then
    /// follows the drone of the station the console has chosen. The scope keeps its own copy of
    /// the controls asset, for the fleet-wide keys and as the template each station copies, and its
    /// lifetime token, which pending requests use, is cancelled when it is destroyed or play mode
    /// exits.
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
            if (dronePrefab == null)
            {
                throw new InvalidOperationException($"{name} has no drone prefab assigned.");
            }
            var fleetSettings = settings.ToSettings();
            ownControls = Instantiate(controls);
            lifetime = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken, Application.exitCancellationToken);
            new FleetInstaller(fleetSettings, ownControls, lifetime.Token, dronePrefab).Install(builder);
            builder.RegisterEntryPoint<FleetRunner>();
            if (followCamera != null)
            {
                builder.RegisterEntryPoint<StationFollowCamera>().AsSelf().WithParameter("camera", followCamera);
            }

            builder.RegisterBuildCallback(container =>
            {
                PlaceGround(fleetSettings.GroundHeight);
                BindHud(container.Resolve<InputDeviceStations>());

                var drones = container.Resolve<DroneInstantiator>();
                foreach (var entry in fleetSettings.Drones)
                {
                    drones.AddDrone(new DroneId(entry.Id), entry.SpawnPosition);
                }
                if (followCamera != null && fleetSettings.Drones.Count > 0)
                {
                    container.Resolve<StationFollowCamera>().Follow(new DroneId(fleetSettings.Drones[0].Id));
                }
            });
        }

        protected override void OnDestroy()
        {
            lifetime?.Cancel();
            base.OnDestroy();
            lifetime?.Dispose();
            if (ownControls != null)
            {
                Destroy(ownControls);
                ownControls = null;
            }
        }

        private void BindHud(InputDeviceStations stations)
        {
            if (debugHud == null)
            {
                return;
            }
            debugHud.Bind(ownControls);
            foreach (var station in stations.Stations)
            {
                debugHud.AddStation(station);
            }
            stations.StationAdded += debugHud.AddStation;
            stations.StationRemoved += debugHud.RemoveStation;
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
    }
}
