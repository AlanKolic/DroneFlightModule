using System;
using System.Collections.Generic;
using DroneFlightModule.Core;

namespace DroneFlightModule.Authorization
{
    /// <summary>
    /// Shared by every drone's grant holder: records which drone each pilot controls, so a pilot
    /// controls at most one drone at a time. Main thread only, like the grant holders that use it.
    /// </summary>
    public sealed class PilotClaims
    {
        private readonly Dictionary<PilotId, DroneId> drones = new Dictionary<PilotId, DroneId>();

        public bool TryGetDrone(PilotId pilot, out DroneId drone)
        {
            return drones.TryGetValue(pilot, out drone);
        }

        public void Claim(PilotId pilot, DroneId drone)
        {
            if (pilot.IsEmpty)
            {
                throw new ArgumentException("Pilot id is required.", nameof(pilot));
            }
            if (drone.IsEmpty)
            {
                throw new ArgumentException("Drone id is required.", nameof(drone));
            }
            if (drones.TryGetValue(pilot, out var claimed) && claimed != drone)
            {
                throw new InvalidOperationException($"{pilot} already controls {claimed}.");
            }
            drones[pilot] = drone;
        }

        public bool Release(PilotId pilot, DroneId drone)
        {
            if (!drones.TryGetValue(pilot, out var claimed) || claimed != drone)
            {
                return false;
            }
            drones.Remove(pilot);
            return true;
        }
    }
}
