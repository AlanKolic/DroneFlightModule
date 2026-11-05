using DroneFlightModule.Core;

namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// What an assignment policy decided: assign a free drone, take one over, or deny. Drone is
    /// set unless the decision is a denial, and Denial only when it is.
    /// </summary>
    public readonly struct AssignmentDecision
    {
        public readonly AssignmentAction Action;
        public readonly DroneId Drone;
        public readonly FlightDenial Denial;

        public AssignmentDecision(AssignmentAction action, DroneId drone)
        {
            Action = action;
            Drone = drone;
            Denial = default;
        }

        public AssignmentDecision(FlightDenial denial)
        {
            Action = AssignmentAction.Deny;
            Drone = default;
            Denial = denial;
        }

        public override string ToString() => Action == AssignmentAction.Deny ? $"deny ({Denial})" : $"{Action} {Drone}";
    }
}
