using DroneFlightModule.Core;
using DroneFlightModule.Unity;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Composition root for the single-drone prototype scene: installs the drone, ticks it every
    /// fixed update and binds the scene's view and ground plane to it.
    /// </summary>
    public sealed class DroneLifetimeScope : LifetimeScope
    {
        [SerializeField] private string droneId = "drone-1";
        [SerializeField] private Vector3 spawnPosition = Vector3.zero;
        [SerializeField] private float groundHeight = 0f;
        [SerializeField] private DroneView droneView;
        [SerializeField] private Transform groundPlane;

        protected override void Configure(IContainerBuilder builder)
        {
            new DroneInstaller(new DroneId(droneId), spawnPosition.ToVec3(), groundHeight).Install(builder);
            builder.RegisterEntryPoint<FlightLoopRunner>();

            builder.RegisterBuildCallback(container =>
            {
                if (groundPlane != null)
                {
                    var position = groundPlane.position;
                    groundPlane.position = new Vector3(position.x, groundHeight, position.z);
                }
                if (droneView != null)
                {
                    droneView.Bind(container.Resolve<FlightLoop>());
                }
            });
        }
    }
}
