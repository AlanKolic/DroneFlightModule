using System;
using System.Collections.Generic;
using DroneFlightModule.Core;

namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// The fleet, in the order drones joined, which is also the order the assignment policy
    /// considers them in. Drones can join at any time, also while pilots fly: a request decides on
    /// the fleet as it is once authorization completes, so a drone that joined meanwhile counts.
    /// Drones leave only through <see cref="FlightCoordinator.RemoveDrone"/>, which releases them
    /// first, so a drone never leaves while someone still flies it.
    /// </summary>
    public sealed class DroneRegistry
    {
        private readonly List<FleetDrone> drones = new List<FleetDrone>();
        private readonly Dictionary<DroneId, FleetDrone> byId = new Dictionary<DroneId, FleetDrone>();

        public DroneRegistry(IEnumerable<FleetDrone> drones)
        {
            if (drones == null)
            {
                throw new ArgumentNullException(nameof(drones));
            }
            foreach (var drone in drones)
            {
                Add(drone);
            }
        }

        public IReadOnlyList<FleetDrone> Drones => drones;

        public void Add(FleetDrone drone)
        {
            if (drone == null)
            {
                throw new ArgumentNullException(nameof(drone));
            }
            if (byId.ContainsKey(drone.Id))
            {
                throw new ArgumentException($"Drone {drone.Id} is already in the fleet.", nameof(drone));
            }
            byId.Add(drone.Id, drone);
            drones.Add(drone);
        }

        internal bool Remove(DroneId id)
        {
            if (!byId.TryGetValue(id, out var drone))
            {
                return false;
            }
            byId.Remove(id);
            drones.Remove(drone);
            return true;
        }

        public bool TryGet(DroneId id, out FleetDrone drone)
        {
            if (id.IsEmpty)
            {
                drone = null;
                return false;
            }
            return byId.TryGetValue(id, out drone);
        }
    }
}
