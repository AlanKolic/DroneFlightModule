using DroneFlightModule.Core;

namespace DroneFlightModule.Pilots
{
    /// <summary>
    /// Where pilot stats are kept. It only stores; <see cref="PilotStatsRecorder"/> decides what
    /// changes. A pilot with no record reads as <see cref="PilotStats.None"/>.
    /// </summary>
    public interface IPilotStatsStore
    {
        PilotStats Get(PilotId pilot);

        void Save(PilotId pilot, PilotStats stats);
    }
}
