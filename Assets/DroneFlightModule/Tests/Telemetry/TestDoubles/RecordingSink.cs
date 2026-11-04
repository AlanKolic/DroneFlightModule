using System.Collections.Generic;
using DroneFlightModule.Core;

namespace DroneFlightModule.Telemetry.Tests
{
    /// <summary>
    /// A sink that remembers every sample and every ended stream, in order.
    /// </summary>
    internal sealed class RecordingSink : ITelemetrySink
    {
        public List<FlightFrame> Written { get; } = new List<FlightFrame>();
        public List<DroneId> Ended { get; } = new List<DroneId>();

        public void Write(in FlightFrame sample)
        {
            Written.Add(sample);
        }

        public void End(DroneId drone)
        {
            Ended.Add(drone);
        }
    }
}
