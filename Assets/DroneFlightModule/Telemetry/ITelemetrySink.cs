using DroneFlightModule.Core;

namespace DroneFlightModule.Telemetry
{
    /// <summary>
    /// Where sampled telemetry goes: memory now, the network later. Write is called on the
    /// simulation's tick, so it must be quick; End says a drone's stream has finished, for example
    /// because it left the fleet.
    /// </summary>
    public interface ITelemetrySink
    {
        void Write(in FlightFrame sample);

        void End(DroneId drone);
    }
}
