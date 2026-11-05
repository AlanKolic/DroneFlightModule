using System;
using UnityEngine.InputSystem;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// The keys that act on the whole fleet rather than on one station's pilot: the Debug keys that
    /// add and remove drones. Each station binds its own keys; the dashboard's toggle stays with
    /// the dashboard. Disposing unbinds them.
    /// </summary>
    public sealed class FleetKeys : IDisposable
    {
        private readonly ActionBindings bindings;

        public FleetKeys(InputActionAsset actions, DroneInstantiator drones)
        {
            if (drones == null)
            {
                throw new ArgumentNullException(nameof(drones));
            }
            bindings = new ActionBindings(actions);
            bindings.OnPress("Debug/AddDrone", () => drones.AddDrone());
            bindings.OnPress("Debug/RemoveDrone", () => drones.RemoveNewestDrone());
        }

        public void Dispose()
        {
            bindings.Dispose();
        }
    }
}
