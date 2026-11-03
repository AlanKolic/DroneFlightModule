using System;
using DroneFlightModule.Core;
using DroneFlightModule.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Composition root for the single-drone prototype scene: installs the drone, ticks it every
    /// fixed update and binds the scene's view, debug HUD and ground plane to it. Each scope flies
    /// with its own copy of the controls asset, so enabling or disabling one drone's actions never
    /// touches another drone's.
    /// </summary>
    public sealed class DroneLifetimeScope : LifetimeScope
    {
        [SerializeField] private string droneId = "drone-1";
        [SerializeField] private Vector3 spawnPosition = Vector3.zero;
        [SerializeField] private float groundHeight = 0f;
        [SerializeField] private InputActionAsset controls;
        [SerializeField] private DroneView droneView;
        [SerializeField] private FlightDebugHud debugHud;
        [SerializeField] private Transform groundPlane;

        private InputActionAsset ownControls;

        protected override void Configure(IContainerBuilder builder)
        {
            if (controls == null)
            {
                throw new InvalidOperationException($"{name} has no controls asset assigned.");
            }
            ownControls = Instantiate(controls);
            new DroneInstaller(new DroneId(droneId), spawnPosition.ToVec3(), groundHeight, ownControls).Install(builder);
            builder.RegisterEntryPoint<FlightLoopRunner>();

            builder.RegisterBuildCallback(container =>
            {
                if (groundPlane != null)
                {
                    var position = groundPlane.position;
                    groundPlane.position = new Vector3(position.x, groundHeight, position.z);
                }
                var loop = container.Resolve<FlightLoop>();
                if (droneView != null)
                {
                    droneView.Bind(loop);
                }
                if (debugHud != null)
                {
                    debugHud.Bind(loop, container.Resolve<InputRouter>(), ownControls);
                }
                // Nothing else depends on the toggle, so resolve it here to start listening for its key.
                container.Resolve<LinkLossToggle>();
            });
        }

        protected override void OnDestroy()
        {
            // The container goes first, so the copy's actions are disabled before it is destroyed.
            base.OnDestroy();
            if (ownControls != null)
            {
                Destroy(ownControls);
                ownControls = null;
            }
        }
    }
}
