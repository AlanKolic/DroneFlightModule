using System;
using System.Collections.Generic;
using DroneFlightModule.Core;

namespace DroneFlightModule.Telemetry
{
    /// <summary>
    /// Keeps the fleet's telemetry in memory: the latest sample of every drone still streaming, and
    /// the most recent samples of all drones together, oldest first. A drone whose stream ends
    /// drops out of the latest set; its samples stay in the history until newer ones push them out.
    /// </summary>
    public sealed class InMemoryTelemetrySink : ITelemetrySink
    {
        private readonly Dictionary<DroneId, FlightFrame> latest = new Dictionary<DroneId, FlightFrame>();
        private readonly RingBuffer<FlightFrame> history;

        public InMemoryTelemetrySink(TelemetryConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            history = new RingBuffer<FlightFrame>(config.FleetHistoryLength);
        }

        public IReadOnlyCollection<DroneId> Drones => latest.Keys;
        public IReadOnlyList<FlightFrame> History => history;

        public bool TryGetLatest(DroneId drone, out FlightFrame sample)
        {
            return latest.TryGetValue(drone, out sample);
        }

        public void Write(in FlightFrame sample)
        {
            latest[sample.DroneId] = sample;
            history.Add(sample);
        }

        public void End(DroneId drone)
        {
            latest.Remove(drone);
        }
    }
}
