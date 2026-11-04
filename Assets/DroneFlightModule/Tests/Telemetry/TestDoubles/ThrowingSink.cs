using System;
using DroneFlightModule.Core;

namespace DroneFlightModule.Telemetry.Tests
{
    /// <summary>
    /// A sink that fails on every call, as a broken network sink might.
    /// </summary>
    internal sealed class ThrowingSink : ITelemetrySink
    {
        public void Write(in FlightFrame sample)
        {
            throw new InvalidOperationException("The sink is broken.");
        }

        public void End(DroneId drone)
        {
            throw new InvalidOperationException("The sink is broken.");
        }
    }
}
