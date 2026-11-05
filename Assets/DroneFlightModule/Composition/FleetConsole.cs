using System;
using System.Collections.Generic;
using System.Linq;
using DroneFlightModule.Core;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// The fleet scene's console: forwards to the chosen pilot station, the first one until
    /// another is chosen, and to the drone instantiator, so a control panel can do everything the
    /// keys do. A request runs on and reports through the station. Changed fires when another
    /// station is chosen or the chosen one's status changes, which the camera follows.
    /// </summary>
    public sealed class FleetConsole : IFleetConsole, IDisposable
    {
        private readonly InputDeviceStations stations;
        private readonly DroneInstantiator drones;
        private PilotStation selected;

        public FleetConsole(InputDeviceStations stations, DroneInstantiator drones)
        {
            this.stations = stations ?? throw new ArgumentNullException(nameof(stations));
            this.drones = drones ?? throw new ArgumentNullException(nameof(drones));
            foreach (var station in stations.Stations)
            {
                OnStationAdded(station);
            }
            stations.StationAdded += OnStationAdded;
        }

        public event Action Changed;

        public IReadOnlyList<string> Stations => stations.Stations.Select(station => station.Name).ToList();
        public string Station => selected?.Name ?? string.Empty;
        public PilotId Pilot => selected?.Session.PilotId ?? default;
        public string LastMessage => selected?.LastMessage ?? "No input device is connected.";
        public bool IsLinkCut => selected != null && selected.Link.IsLinkCut;

        public void SelectStation(string station)
        {
            var chosen = stations.Stations.FirstOrDefault(candidate => candidate.Name == station)
                ?? throw new ArgumentException($"There is no station {station}.", nameof(station));
            if (chosen == selected)
            {
                return;
            }
            selected = chosen;
            Changed?.Invoke();
        }

        public bool TryGetDrone(out DroneId drone)
        {
            drone = default;
            return selected != null && selected.TryGetDrone(out drone);
        }

        public void SelectPilot(PilotId pilot)
        {
            selected?.SelectPilot(pilot);
        }

        public void RequestFlight(DroneId? drone)
        {
            // The request never faults, so nothing is lost by not awaiting it.
            if (selected != null)
            {
                _ = selected.RequestFlightAsync(drone);
            }
        }

        public void Release()
        {
            selected?.Release();
        }

        public void RevokeGrant()
        {
            selected?.RevokeServerGrant();
        }

        public void ToggleLink()
        {
            selected?.ToggleLink();
        }

        public void AddDrone()
        {
            drones.AddDrone();
        }

        public void RemoveDrone(DroneId drone)
        {
            drones.RemoveDrone(drone);
        }

        public void Dispose()
        {
            stations.StationAdded -= OnStationAdded;
            foreach (var station in stations.Stations)
            {
                station.StatusChanged -= OnStatusChanged;
            }
        }

        private void OnStationAdded(PilotStation station)
        {
            station.StatusChanged += OnStatusChanged;
            if (selected == null)
            {
                selected = station;
                Changed?.Invoke();
            }
        }

        private void OnStatusChanged(PilotStation station)
        {
            if (station == selected)
            {
                Changed?.Invoke();
            }
        }
    }
}
