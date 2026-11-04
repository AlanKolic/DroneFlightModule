using System;
using System.Collections.Generic;
using DroneFlightModule.Core;

namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// The fleet, fixed at construction and kept in the given order, which is also the order the
    /// assignment policy considers drones in.
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
                if (drone == null)
                {
                    throw new ArgumentException("Drones cannot be null.", nameof(drones));
                }
                if (byId.ContainsKey(drone.Id))
                {
                    throw new ArgumentException($"Drone {drone.Id} is registered twice.", nameof(drones));
                }
                byId.Add(drone.Id, drone);
                this.drones.Add(drone);
            }
            if (this.drones.Count == 0)
            {
                throw new ArgumentException("At least one drone is required.", nameof(drones));
            }
        }

        public IReadOnlyList<FleetDrone> Drones => drones;

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
