using System;
using System.Collections.Generic;
using System.Threading;
using DroneFlightModule.Coordination;
using DroneFlightModule.Core;
using DroneFlightModule.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Composition root of the fleet scene. Installs what the fleet shares and the
    /// <see cref="FleetConsole"/> a control panel drives, then has the
    /// <see cref="DroneInstantiator"/> add the configured drones; the Debug/AddDrone and
    /// Debug/RemoveDrone keys add a drone from the prefab and remove the newest one while the
    /// fleet runs. The local pilot starts as the guest without a drone; the HUD shows every drone
    /// and the local pilot, and the camera follows the drone they fly. The scope flies with its own
    /// copy of the controls asset, and its lifetime token, which pending requests use, is cancelled
    /// when it is destroyed or play mode exits.
    /// </summary>
    public sealed class FleetLifetimeScope : LifetimeScope
    {
        [SerializeField] private float groundHeight = 0f;
        [SerializeField] private float serverLatencySeconds = 0.5f;
        [SerializeField] private InputActionAsset controls;
        [SerializeField] private FleetDroneEntry[] drones = new FleetDroneEntry[0];
        [SerializeField] private DroneView dronePrefab;
        [SerializeField] private Vector3 spawnStep = new Vector3(4f, 0f, 0f);
        [SerializeField] private FlightDebugHud debugHud;
        [SerializeField] private FollowCamera followCamera;
        [SerializeField] private Transform groundPlane;

        private readonly List<(InputAction Action, Action<InputAction.CallbackContext> Handler)> debugKeys =
            new List<(InputAction Action, Action<InputAction.CallbackContext> Handler)>();
        private InputActionAsset ownControls;
        private CancellationTokenSource lifetime;
        private LocalPilotControls localPilot;
        private Transform followed;

        public DroneInstantiator Instantiator { get; private set; }

        protected override void Configure(IContainerBuilder builder)
        {
            if (controls == null)
            {
                throw new InvalidOperationException($"{name} has no controls asset assigned.");
            }
            ownControls = Instantiate(controls);
            lifetime = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken, Application.exitCancellationToken);
            var fleet = new FleetInstaller(groundHeight, ownControls, TimeSpan.FromSeconds(serverLatencySeconds), lifetime.Token);
            fleet.Install(builder);
            builder.RegisterEntryPoint<SimulationClockRunner>();
            builder.Register<DroneInstantiator>(Lifetime.Singleton)
                .WithParameter("prefab", dronePrefab)
                .WithParameter("hud", debugHud)
                .WithParameter("groundHeight", groundHeight)
                .WithParameter("spawnStep", spawnStep);
            builder.Register<IFleetConsole, FleetConsole>(Lifetime.Singleton);

            builder.RegisterBuildCallback(container =>
            {
                PlaceGround();
                if (debugHud != null)
                {
                    debugHud.Bind(ownControls);
                }
                
                Instantiator = container.Resolve<DroneInstantiator>();
                foreach (var entry in drones)
                {
                    Instantiator.AddDrone(new DroneId(entry.Id), entry.SpawnPosition, entry.View);
                }
                
                fleet.Start(container);
                localPilot = container.Resolve<LocalPilotControls>();
                localPilot.StatusChanged += OnLocalPilotChanged;
                OnLocalPilotChanged();

                OnDebugKey("Debug/AddDrone", () => Instantiator.AddDrone());
                OnDebugKey("Debug/RemoveDrone", () => Instantiator.RemoveNewestDrone());
            });
        }

        protected override void OnDestroy()
        {
            // Unhooked first: disposing the fleet releases drones, and the views may already be gone.
            if (localPilot != null)
            {
                localPilot.StatusChanged -= OnLocalPilotChanged;
            }
            foreach (var key in debugKeys)
            {
                key.Action.performed -= key.Handler;
                key.Action.Disable();
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

        private void PlaceGround()
        {
            if (groundPlane == null)
            {
                return;
            }
            var position = groundPlane.position;
            groundPlane.position = new Vector3(position.x, groundHeight, position.z);
        }

        private void OnDebugKey(string action, Action press)
        {
            Action<InputAction.CallbackContext> handler = _ => press();
            var inputAction = ownControls.FindAction(action, throwIfNotFound: true);
            inputAction.performed += handler;
            inputAction.Enable();
            debugKeys.Add((inputAction, handler));
        }

        private void OnLocalPilotChanged()
        {
            var session = localPilot.Session;
            bool isFlying = localPilot.TryGetDrone(out var drone);
            if (debugHud != null)
            {
                string flying = isFlying ? $"flying {drone}" : "no drone";
                debugHud.SetPilotStatus($"You: {session.PilotId} via {session.Provider.Id}, {flying}\n{localPilot.LastMessage}");
            }
            // Compared by transform: a removed drone's id can come back on a new drone.
            if (isFlying && followCamera != null && Instantiator.TryGetView(drone, out var view) && view.transform != followed)
            {
                followed = view.transform;
                followCamera.SetTarget(followed);
            }
        }
    }
}
