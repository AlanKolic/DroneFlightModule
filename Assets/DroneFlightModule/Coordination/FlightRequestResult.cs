using DroneFlightModule.Core;

namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// The coordinator's answer to a flight request: the drone the pilot now flies, the pilot it
    /// was taken from on a takeover, or why the request was denied, with a readable message.
    /// </summary>
    public sealed class FlightRequestResult
    {
        public FlightRequestResult(DroneId drone)
        {
            Outcome = FlightRequestOutcome.Assigned;
            Drone = drone;
            Message = $"Flying {drone}.";
        }

        public FlightRequestResult(DroneId drone, PilotId fromPilot)
        {
            Outcome = FlightRequestOutcome.TookOver;
            Drone = drone;
            FromPilot = fromPilot;
            Message = $"Took over {drone} from {fromPilot}.";
        }

        public FlightRequestResult(FlightDenial denial, string message)
        {
            Outcome = FlightRequestOutcome.Denied;
            Denial = denial;
            Message = message;
        }

        public FlightRequestOutcome Outcome { get; }
        public DroneId Drone { get; }
        public PilotId FromPilot { get; }
        public FlightDenial Denial { get; }
        public string Message { get; }

        public override string ToString() => Message;
    }
}
