using System;
using DroneFlightModule.Core;
using DroneFlightModule.Unity;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Points the follow camera at the drone of the station the console has chosen, whenever that
    /// changes. Disposing stops following.
    /// </summary>
    public sealed class StationFollowCamera : IDisposable
    {
        private readonly FleetConsole console;
        private readonly DroneInstantiator drones;
        private readonly FollowCamera camera;

        public StationFollowCamera(FleetConsole console, DroneInstantiator drones, FollowCamera camera)
        {
            this.console = console ?? throw new ArgumentNullException(nameof(console));
            this.drones = drones ?? throw new ArgumentNullException(nameof(drones));
            this.camera = camera ?? throw new ArgumentNullException(nameof(camera));
            console.Changed += OnConsoleChanged;
        }

        public void Follow(DroneId drone)
        {
            // Compared by transform: a removed drone's id can come back on a new drone.
            if (drones.TryGetView(drone, out var view) && view.transform != camera.Target)
            {
                camera.SetTarget(view.transform);
            }
        }

        public void Dispose()
        {
            console.Changed -= OnConsoleChanged;
        }

        private void OnConsoleChanged()
        {
            if (console.TryGetDrone(out var drone))
            {
                Follow(drone);
            }
        }
    }
}
