using DroneFlightModule.Core;

namespace DroneFlightModule.Telemetry
{
    /// <summary>
    /// How telemetry is kept: samples per second of simulation time, how many recent samples each
    /// drone's recorder keeps, and how many the fleet-wide in-memory sink keeps for all drones
    /// together.
    /// </summary>
    public sealed class TelemetryConfig
    {
        public static readonly TelemetryConfig Default = new TelemetryConfig(
            sampleRate: 10f,
            droneHistoryLength: 600,
            fleetHistoryLength: 3000);

        private readonly ArgumentGuard guard = new ArgumentGuard();

        public float SampleRate { get; }
        public int DroneHistoryLength { get; }
        public int FleetHistoryLength { get; }

        public TelemetryConfig(float sampleRate, int droneHistoryLength, int fleetHistoryLength)
        {
            SampleRate = guard.RequirePositive(sampleRate, nameof(sampleRate));
            DroneHistoryLength = guard.RequirePositive(droneHistoryLength, nameof(droneHistoryLength));
            FleetHistoryLength = guard.RequirePositive(fleetHistoryLength, nameof(fleetHistoryLength));
        }
    }
}
