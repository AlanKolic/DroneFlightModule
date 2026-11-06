using DroneFlightModule.Core;

namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// A pilot's flight request or arm was refused. Preferred is the drone they asked for, or null for any.
    /// </summary>
    public readonly struct FlightDenied
    {
        public readonly PilotId Pilot;
        public readonly DroneId? Preferred;
        public readonly FlightDenial Denial;
        public readonly string Message;

        public FlightDenied(PilotId pilot, DroneId? preferred, FlightDenial denial, string message)
        {
            Pilot = pilot;
            Preferred = preferred;
            Denial = denial;
            Message = message;
        }
    }
}
