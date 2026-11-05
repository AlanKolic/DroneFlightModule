using System.Collections.Generic;
using DroneFlightModule.Core;

namespace DroneFlightModule.Telemetry
{
    /// <summary>
    /// Keeps the latest sample of every drone still streaming, for the dashboard. A drone whose
    /// stream ends drops out.
    /// </summary>
    public sealed class InMemoryTelemetrySink : ITelemetrySink
    {
        private readonly Dictionary<DroneId, FlightFrame> latest = new Dictionary<DroneId, FlightFrame>();

        public bool TryGetLatest(DroneId drone, out FlightFrame sample)
        {
            return latest.TryGetValue(drone, out sample);
        }

        public void Write(in FlightFrame sample)
        {
            latest[sample.DroneId] = sample;
        }

        public void End(DroneId drone)
        {
            latest.Remove(drone);
        }
    }
}
