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
    /// fixed update and binds the scene's view, debug HUD and ground plane to it.
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

        protected override void Configure(IContainerBuilder builder)
        {
            new DroneInstaller(new DroneId(droneId), spawnPosition.ToVec3(), groundHeight, controls).Install(builder);
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
                    debugHud.Bind(loop, container.Resolve<InputRouter>());
                }
                // Nothing else depends on the toggle, so resolve it here to start listening for its key.
                container.Resolve<LinkLossToggle>();
            });
        }
    }
}
