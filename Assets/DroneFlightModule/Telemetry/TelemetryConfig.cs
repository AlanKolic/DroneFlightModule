using DroneFlightModule.Core;

namespace DroneFlightModule.Telemetry
{
    /// <summary>
    /// How telemetry is kept: samples per second of simulation time.
    /// </summary>
    public sealed class TelemetryConfig
    {
        public static readonly TelemetryConfig Default = new TelemetryConfig(sampleRate: 10f);

        private readonly ArgumentGuard guard = new ArgumentGuard();

        public float SampleRate { get; }

        public TelemetryConfig(float sampleRate)
        {
            SampleRate = guard.RequirePositive(sampleRate, nameof(sampleRate));
        }
    }
}
