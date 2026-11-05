using System;
using System.Collections.Generic;
using System.Linq;
using DroneFlightModule.Core;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// The fleet scene's console: forwards to the chosen pilot station, the first one until
    /// another is chosen, and to the drone instantiator, so a control panel can do everything the
    /// keys do. A request runs on and reports through the station.
    /// </summary>
    public sealed class FleetConsole : IFleetConsole
    {
        private readonly InputDeviceStations stations;
        private readonly DroneInstantiator drones;
        private PilotStation chosen;

        public FleetConsole(InputDeviceStations stations, DroneInstantiator drones)
        {
            this.stations = stations ?? throw new ArgumentNullException(nameof(stations));
            this.drones = drones ?? throw new ArgumentNullException(nameof(drones));
        }

        public IReadOnlyList<string> Stations => stations.Stations.Select(station => station.Name).ToList();
        public string Station => Selected?.Name ?? string.Empty;
        public PilotId Pilot => Selected?.Session.PilotId ?? default;
        public string LastMessage => Selected?.LastMessage ?? "No input device is connected.";
        public bool IsLinkCut => Selected != null && Selected.Link.IsLinkCut;
        public string LinkStatus => Selected?.LinkStatus ?? string.Empty;

        // Read each time, so a station plugged in when there was none is picked up.
        private PilotStation Selected => chosen ?? (chosen = stations.Stations.FirstOrDefault());

        public void SelectStation(string station)
        {
            chosen = stations.Stations.FirstOrDefault(candidate => candidate.Name == station)
                ?? throw new ArgumentException($"There is no station {station}.", nameof(station));
        }

        public bool TryGetDrone(out DroneId drone)
        {
            drone = default;
            return Selected != null && Selected.TryGetDrone(out drone);
        }

        public void SelectPilot(PilotId pilot)
        {
            Selected?.SelectPilot(pilot);
        }

        public void RequestFlight(DroneId? drone)
        {
            // The request never faults, so nothing is lost by not awaiting it.
            if (Selected != null)
            {
                _ = Selected.RequestFlightAsync(drone);
            }
        }

        public void Release()
        {
            Selected?.Release();
        }

        public void RevokeGrant()
        {
            Selected?.RevokeServerGrant();
        }

        public void ToggleLink()
        {
            Selected?.ToggleLink();
        }

        public void AddDrone()
        {
            drones.AddDrone();
        }

        public void RemoveDrone(DroneId drone)
        {
            drones.RemoveDrone(drone);
        }
    }
}
