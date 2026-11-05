using System;
using DroneFlightModule.Core;
using DroneFlightModule.Unity;
using VContainer.Unity;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Each frame, keeps the follow camera on the drone of the station the console has chosen; with
    /// no drone it stays where it is.
    /// </summary>
    public sealed class StationFollowCamera : ITickable
    {
        private readonly IFleetConsole console;
        private readonly DroneInstantiator drones;
        private readonly FollowCamera camera;

        public StationFollowCamera(IFleetConsole console, DroneInstantiator drones, FollowCamera camera)
        {
            this.console = console ?? throw new ArgumentNullException(nameof(console));
            this.drones = drones ?? throw new ArgumentNullException(nameof(drones));
            this.camera = camera ?? throw new ArgumentNullException(nameof(camera));
        }

        public void Tick()
        {
            if (console.TryGetDrone(out var drone))
            {
                Follow(drone);
            }
        }

        public void Follow(DroneId drone)
        {
            // Compared by transform: a removed drone's id can come back on a new drone.
            if (drones.TryGetView(drone, out var view) && view.transform != camera.Target)
            {
                camera.SetTarget(view.transform);
            }
        }
    }
}
