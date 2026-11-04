using System;
using System.Globalization;
using System.Text;
using DroneFlightModule.Coordination;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;
using DroneFlightModule.Telemetry;

namespace DroneFlightModule.UI
{
    /// <summary>
    /// Turns the fleet's state into the dashboard's text: each drone with its pilot and their
    /// priority, mode and countdown, measured height with the true height alongside, position,
    /// velocity and heading from its latest telemetry; each pilot with their drone and stats; and
    /// the latest events, newest first. Only reads. Invariant culture keeps the decimal point stable.
    /// </summary>
    public sealed class FleetDashboardReadout
    {
        private const double RadiansToDegrees = 180.0 / Math.PI;

        private readonly DroneRegistry drones;
        private readonly FlightCoordinator coordinator;
        private readonly IPilotRegistry pilots;
        private readonly IPilotStatsStore stats;
        private readonly InMemoryTelemetrySink telemetry;
        private readonly ITerrain terrain;
        private readonly FleetEventLog events;

        public FleetDashboardReadout(
            DroneRegistry drones,
            FlightCoordinator coordinator,
            IPilotRegistry pilots,
            IPilotStatsStore stats,
            InMemoryTelemetrySink telemetry,
            ITerrain terrain,
            FleetEventLog events)
        {
            this.drones = drones ?? throw new ArgumentNullException(nameof(drones));
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            this.pilots = pilots ?? throw new ArgumentNullException(nameof(pilots));
            this.stats = stats ?? throw new ArgumentNullException(nameof(stats));
            this.telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
            this.terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            this.events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public string Drones()
        {
            var text = new StringBuilder();
            foreach (var drone in drones.Drones)
            {
                if (text.Length > 0)
                {
                    text.Append('\n');
                }
                text.Append(drone.Id).Append("   ").Append(PilotOf(drone.Id)).Append('\n');
                if (telemetry.TryGetLatest(drone.Id, out var sample))
                {
                    AppendTelemetry(text, sample);
                }
                else
                {
                    text.Append("  no telemetry yet\n");
                }
            }
            return text.Length > 0 ? text.ToString().TrimEnd('\n') : "No drones in the fleet.";
        }

        public string Pilots()
        {
            var text = new StringBuilder();
            foreach (var profile in pilots.Profiles)
            {
                var record = stats.Get(profile.Id);
                string flying = coordinator.TryGetDrone(profile.Id, out var drone) ? $"flying {drone}" : "not flying";
                text.Append(Invariant(
                    $"{profile.DisplayName} ({profile.Id}, priority {profile.Priority}): {flying}, flights {record.Flights}, {record.FlightTime:0.0} s, takeovers {record.Takeovers}, preempted {record.TimesPreempted}\n"));
            }
            return text.ToString().TrimEnd('\n');
        }

        public string Events(int count)
        {
            var lines = events.Lines;
            if (lines.Count == 0)
            {
                return "Nothing yet.";
            }
            var text = new StringBuilder();
            for (int i = lines.Count - 1; i >= Math.Max(0, lines.Count - count); i--)
            {
                text.Append(lines[i]).Append('\n');
            }
            return text.ToString().TrimEnd('\n');
        }

        private string PilotOf(DroneId drone)
        {
            if (!coordinator.TryGetPilot(drone, out var pilot))
            {
                return "free";
            }
            return pilots.TryGet(pilot, out var profile) ? $"{pilot} (priority {profile.Priority})" : pilot.ToString();
        }

        private void AppendTelemetry(StringBuilder text, in FlightFrame sample)
        {
            var state = sample.State;
            float trueHeight = state.Position.Y - terrain.HeightAt(state.Position.X, state.Position.Z);
            string measured = sample.Altitude.IsValid ? Invariant($"{sample.Altitude.HeightAboveGround:0.00} m") : "out of range";
            // Rounded before wrapping, so a yaw just left of north shows 0°, never 360°.
            double heading = (Math.Round(state.Yaw * RadiansToDegrees) % 360.0 + 360.0) % 360.0;

            text.Append("  ").Append(Mode(sample)).Append(Invariant($", height {measured} (true {trueHeight:0.00} m)\n"));
            text.Append(Invariant($"  position {Format(state.Position)}, velocity {Format(state.Velocity)}, heading {heading:0}°\n"));
        }

        private string Mode(in FlightFrame sample)
        {
            return sample.Mode == FlightMode.Holding
                ? Invariant($"Holding, descending in {sample.HoldTimeRemaining:0.0} s")
                : sample.Mode.ToString();
        }

        private string Format(Vec3 vector)
        {
            return Invariant($"({vector.X:0.0}, {vector.Y:0.0}, {vector.Z:0.0})");
        }

        private string Invariant(FormattableString text)
        {
            return text.ToString(CultureInfo.InvariantCulture);
        }
    }
}
