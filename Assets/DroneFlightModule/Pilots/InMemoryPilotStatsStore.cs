using System.Collections.Generic;
using DroneFlightModule.Core;

namespace DroneFlightModule.Pilots
{
    /// <summary>
    /// Keeps stats for the lifetime of the process. A file or server backed store is a new implementation.
    /// </summary>
    public sealed class InMemoryPilotStatsStore : IPilotStatsStore
    {
        private readonly ArgumentGuard guard = new ArgumentGuard();
        private readonly Dictionary<PilotId, PilotStats> stats = new Dictionary<PilotId, PilotStats>();

        public PilotStats Get(PilotId pilot)
        {
            return stats.TryGetValue(pilot, out var value) ? value : PilotStats.None;
        }

        public void Save(PilotId pilot, PilotStats stats)
        {
            guard.RequireId(pilot, nameof(pilot));
            this.stats[pilot] = stats;
        }
    }
}
