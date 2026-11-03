using System;
using System.Collections.Generic;
using DroneFlightModule.Core;

namespace DroneFlightModule.Pilots
{
    /// <summary>
    /// Turns coordinator events into stats. A flight runs from the pilot getting a drone to losing
    /// it, for whatever reason, so a takeover ends one pilot's flight and starts another's. A pilot
    /// flies one drone at a time. Times are simulation seconds.
    /// </summary>
    public sealed class PilotStatsRecorder
    {
        private readonly ArgumentGuard guard = new ArgumentGuard();
        private readonly IPilotStatsStore store;
        private readonly Dictionary<PilotId, (DroneId Drone, double Started)> openFlights =
            new Dictionary<PilotId, (DroneId Drone, double Started)>();

        public PilotStatsRecorder(IPilotStatsStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public bool IsFlying(PilotId pilot, DroneId drone) => openFlights.TryGetValue(pilot, out var flight) && flight.Drone == drone;

        public void FlightStarted(PilotId pilot, DroneId drone, double time)
        {
            guard.RequireId(pilot, nameof(pilot));
            guard.RequireId(drone, nameof(drone));
            if (openFlights.TryGetValue(pilot, out var current))
            {
                throw new InvalidOperationException($"{pilot} is already flying {current.Drone}.");
            }
            openFlights.Add(pilot, (drone, time));
            store.Save(pilot, store.Get(pilot).WithFlightStarted(time));
        }

        public void FlightEnded(PilotId pilot, DroneId drone, double time)
        {
            guard.RequireId(pilot, nameof(pilot));
            guard.RequireId(drone, nameof(drone));
            if (!openFlights.TryGetValue(pilot, out var flight) || flight.Drone != drone)
            {
                throw new InvalidOperationException($"{pilot} is not flying {drone}.");
            }
            double started = flight.Started;
            if (time < started)
            {
                throw new ArgumentOutOfRangeException(nameof(time), time, "A flight cannot end before it started.");
            }
            openFlights.Remove(pilot);
            store.Save(pilot, store.Get(pilot).WithFlightTime(time - started));
        }

        public void TookOver(PilotId pilot)
        {
            guard.RequireId(pilot, nameof(pilot));
            store.Save(pilot, store.Get(pilot).WithTakeover());
        }

        public void Preempted(PilotId pilot)
        {
            guard.RequireId(pilot, nameof(pilot));
            store.Save(pilot, store.Get(pilot).WithPreemption());
        }
    }
}
