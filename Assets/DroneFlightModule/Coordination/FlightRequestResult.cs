using DroneFlightModule.Core;

namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// The coordinator's answer to a flight request: the drone the pilot now flies, the pilot it
    /// was taken from on a takeover, or why the request was denied, with a readable message.
    /// </summary>
    public sealed class FlightRequestResult
    {
        private FlightRequestResult(FlightRequestOutcome outcome, DroneId drone, PilotId fromPilot, FlightDenial denial, string message)
        {
            Outcome = outcome;
            Drone = drone;
            FromPilot = fromPilot;
            Denial = denial;
            Message = message;
        }

        public FlightRequestOutcome Outcome { get; }
        public DroneId Drone { get; }
        public PilotId FromPilot { get; }
        public FlightDenial Denial { get; }
        public string Message { get; }
        public bool IsFlying => Outcome != FlightRequestOutcome.Denied;

        public static FlightRequestResult Assigned(DroneId drone) =>
            new FlightRequestResult(FlightRequestOutcome.Assigned, drone, default, default, $"Flying {drone}.");

        public static FlightRequestResult TookOver(DroneId drone, PilotId fromPilot) =>
            new FlightRequestResult(FlightRequestOutcome.TookOver, drone, fromPilot, default, $"Took over {drone} from {fromPilot}.");

        public static FlightRequestResult Denied(FlightDenial denial, string message) =>
            new FlightRequestResult(FlightRequestOutcome.Denied, default, default, denial, message);

        public override string ToString() => Message;
    }
}
