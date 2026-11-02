using System;
using System.Collections.Generic;
using DroneFlightModule.Core;

namespace DroneFlightModule.Pilots
{
    /// <summary>
    /// Turns coordinator events into stats. A flight runs from the pilot getting a drone to losing
    /// it, for whatever reason, so a takeover ends one pilot's flight and starts another's. One pilot
    /// may fly several drones; each drone is its own flight. Times are simulation seconds.
    /// </summary>
    public sealed class PilotStatsRecorder
    {
        private readonly IPilotStatsStore store;
        private readonly Dictionary<(PilotId Pilot, DroneId Drone), double> openFlights =
            new Dictionary<(PilotId Pilot, DroneId Drone), double>();

        public PilotStatsRecorder(IPilotStatsStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public bool IsFlying(PilotId pilot, DroneId drone) => openFlights.ContainsKey((pilot, drone));

        public void FlightStarted(PilotId pilot, DroneId drone, double time)
        {
            RequireIds(pilot, drone);
            if (!openFlights.TryAdd((pilot, drone), time))
            {
                throw new InvalidOperationException($"{pilot} is already flying {drone}.");
            }
            var stats = store.Get(pilot);
            store.Save(pilot, new PilotStats(stats.Flights + 1, stats.FlightTime, stats.Takeovers, stats.TimesPreempted, time));
        }

        public void FlightEnded(PilotId pilot, DroneId drone, double time)
        {
            RequireIds(pilot, drone);
            if (!openFlights.TryGetValue((pilot, drone), out double started))
            {
                throw new InvalidOperationException($"{pilot} is not flying {drone}.");
            }
            if (time < started)
            {
                throw new ArgumentOutOfRangeException(nameof(time), time, "A flight cannot end before it started.");
            }
            openFlights.Remove((pilot, drone));
            var stats = store.Get(pilot);
            store.Save(pilot, new PilotStats(stats.Flights, stats.FlightTime + (time - started), stats.Takeovers, stats.TimesPreempted, stats.LastFlight));
        }

        public void TookOver(PilotId pilot)
        {
            RequirePilot(pilot);
            var stats = store.Get(pilot);
            store.Save(pilot, new PilotStats(stats.Flights, stats.FlightTime, stats.Takeovers + 1, stats.TimesPreempted, stats.LastFlight));
        }

        public void Preempted(PilotId pilot)
        {
            RequirePilot(pilot);
            var stats = store.Get(pilot);
            store.Save(pilot, new PilotStats(stats.Flights, stats.FlightTime, stats.Takeovers, stats.TimesPreempted + 1, stats.LastFlight));
        }

        private void RequireIds(PilotId pilot, DroneId drone)
        {
            RequirePilot(pilot);
            if (drone.IsEmpty)
            {
                throw new ArgumentException("Drone id is required.", nameof(drone));
            }
        }

        private void RequirePilot(PilotId pilot)
        {
            if (pilot.IsEmpty)
            {
                throw new ArgumentException("Pilot id is required.", nameof(pilot));
            }
        }
    }
}
