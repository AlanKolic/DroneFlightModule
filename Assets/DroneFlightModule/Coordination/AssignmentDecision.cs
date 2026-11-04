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

        private AssignmentDecision(AssignmentAction action, DroneId drone, FlightDenial denial)
        {
            Action = action;
            Drone = drone;
            Denial = denial;
        }

        public static AssignmentDecision Assign(DroneId drone) => new AssignmentDecision(AssignmentAction.Assign, drone, default);

        public static AssignmentDecision TakeOver(DroneId drone) => new AssignmentDecision(AssignmentAction.TakeOver, drone, default);

        public static AssignmentDecision Deny(FlightDenial denial) => new AssignmentDecision(AssignmentAction.Deny, default, denial);

        public override string ToString() => Action == AssignmentAction.Deny ? $"deny ({Denial})" : $"{Action} {Drone}";
    }
}
