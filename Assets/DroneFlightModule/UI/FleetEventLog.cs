using System;
using System.Collections.Generic;
using System.Globalization;
using DroneFlightModule.Coordination;
using DroneFlightModule.Core;

namespace DroneFlightModule.UI
{
    /// <summary>
    /// The fleet's recent coordinator events as readable lines, oldest first, each stamped with
    /// simulation time: assignments, releases and why, takeovers, moves between devices and denials. Keeps a fixed number
    /// of lines. Disposing stops listening.
    /// </summary>
    public sealed class FleetEventLog : IDisposable
    {
        private readonly FlightCoordinator coordinator;
        private readonly ISimulationClock clock;
        private readonly ArgumentGuard guard = new ArgumentGuard();
        private readonly List<string> lines = new List<string>();
        private readonly int capacity;

        public FleetEventLog(FlightCoordinator coordinator, ISimulationClock clock, int capacity)
        {
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.capacity = guard.RequirePositive(capacity, nameof(capacity));
            coordinator.Assigned += OnAssigned;
            coordinator.Released += OnReleased;
            coordinator.TookOver += OnTookOver;
            coordinator.Moved += OnMoved;
            coordinator.Denied += OnDenied;
        }

        public IReadOnlyList<string> Lines => lines;

        public void Dispose()
        {
            coordinator.Assigned -= OnAssigned;
            coordinator.Released -= OnReleased;
            coordinator.TookOver -= OnTookOver;
            coordinator.Moved -= OnMoved;
            coordinator.Denied -= OnDenied;
        }

        private void OnAssigned(DroneAssigned assigned)
        {
            Add($"{assigned.Pilot} got {assigned.Drone}");
        }

        private void OnReleased(DroneReleased released)
        {
            Add(Describe(released));
        }

        private void OnTookOver(DroneTakenOver takenOver)
        {
            Add($"{takenOver.Pilot} took over {takenOver.Drone} from {takenOver.FromPilot}");
        }

        private void OnMoved(DroneMoved moved)
        {
            Add($"{moved.Pilot} moved {moved.Drone} to another device");
        }

        private void OnDenied(FlightDenied denied)
        {
            Add($"{denied.Pilot} denied: {denied.Message}");
        }

        private string Describe(in DroneReleased released)
        {
            switch (released.Reason)
            {
                case ReleaseReason.SessionEnded:
                    return $"{released.Pilot} left the desk, releasing {released.Drone}";
                case ReleaseReason.LinkLost:
                    return $"{released.Pilot} lost {released.Drone}: it landed with no link";
                case ReleaseReason.Revoked:
                    return $"{released.Pilot} lost {released.Drone}: grant revoked";
                case ReleaseReason.Switched:
                    return $"{released.Pilot} left {released.Drone} for another drone";
                case ReleaseReason.Removed:
                    return $"{released.Drone} left the fleet, so {released.Pilot} lost it";
                default:
                    return $"{released.Pilot} released {released.Drone}";
            }
        }

        private void Add(string text)
        {
            if (lines.Count == capacity)
            {
                lines.RemoveAt(0);
            }
            lines.Add(string.Format(CultureInfo.InvariantCulture, "{0:0.0} s  {1}", clock.Time, text));
        }
    }
}
