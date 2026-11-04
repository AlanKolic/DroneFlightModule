using System;
using System.Collections.Generic;
using DroneFlightModule.Coordination;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;

namespace DroneFlightModule.UI
{
    /// <summary>
    /// What the control panel offers: every registered pilot, highest priority first, and any drone
    /// or one of the fleet's drones, in the order they joined. Labels say who flies what, so a
    /// takeover shows in both lists, and a pilot's record counts their takeovers and preemptions.
    /// It remembers the chosen drone, and reads as any drone while that drone is not in the fleet.
    /// </summary>
    public sealed class ControlPanelChoices
    {
        private readonly IPilotRegistry pilots;
        private readonly DroneRegistry drones;
        private readonly FlightCoordinator coordinator;
        private readonly IPilotStatsStore stats;
        private DroneId? chosen;

        public ControlPanelChoices(IPilotRegistry pilots, DroneRegistry drones, FlightCoordinator coordinator, IPilotStatsStore stats)
        {
            this.pilots = pilots ?? throw new ArgumentNullException(nameof(pilots));
            this.drones = drones ?? throw new ArgumentNullException(nameof(drones));
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            this.stats = stats ?? throw new ArgumentNullException(nameof(stats));
        }

        public IReadOnlyList<PilotProfile> Pilots => pilots.Profiles;
        public IReadOnlyList<FleetDrone> Drones => drones.Drones;
        public DroneId? SelectedDrone => chosen.HasValue && drones.TryGet(chosen.Value, out _) ? chosen : null;

        public void SelectDrone(DroneId? drone)
        {
            if (drone.HasValue && !drones.TryGet(drone.Value, out _))
            {
                throw new ArgumentException($"{drone} is not in the fleet.", nameof(drone));
            }
            chosen = drone;
        }

        public string Label(DroneId? drone)
        {
            if (!drone.HasValue)
            {
                return "Any drone";
            }
            return coordinator.TryGetPilot(drone.Value, out var pilot) ? $"{drone.Value}, flown by {pilot}" : $"{drone.Value}, free";
        }

        public string Label(PilotId pilot)
        {
            string name = pilots.TryGet(pilot, out var profile) ? $"{profile.DisplayName} (priority {profile.Priority})" : pilot.ToString();
            return coordinator.TryGetDrone(pilot, out var drone) ? $"{name}, flying {drone}" : name;
        }

        public string Record(PilotId pilot)
        {
            var record = stats.Get(pilot);
            return $"takeovers {record.Takeovers}, preempted {record.TimesPreempted}";
        }
    }
}
