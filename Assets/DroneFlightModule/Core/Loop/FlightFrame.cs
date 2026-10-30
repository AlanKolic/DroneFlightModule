namespace DroneFlightModule.Core
{
    /// <summary>
    /// Snapshot of one completed tick, published to observers such as telemetry.
    /// </summary>
    public readonly struct FlightFrame
    {
        public readonly DroneId DroneId;

        /// <summary>Simulation time in seconds at the end of the tick.</summary>
        public readonly double Time;

        public readonly ControlInput Input;
        public readonly ForceCommand Command;
        public readonly DroneState State;

        public FlightFrame(DroneId droneId, double time, ControlInput input, ForceCommand command, DroneState state)
        {
            DroneId = droneId;
            Time = time;
            Input = input;
            Command = command;
            State = state;
        }
    }
}
