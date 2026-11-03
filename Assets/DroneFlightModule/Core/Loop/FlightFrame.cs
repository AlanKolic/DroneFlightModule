namespace DroneFlightModule.Core
{
    /// <summary>
    /// Snapshot of one completed tick, published to observers such as telemetry.
    /// Time and State are at the end of the tick; Altitude was sampled at its start, before the step.
    /// Command is what reached the actuators, which is the failsafe's own command while it flies.
    /// Mode was decided from the state at the start of the tick, so on the tick that lands or
    /// lifts off, State already shows the new contact while Mode catches up on the next tick.
    /// </summary>
    public readonly struct FlightFrame
    {
        public readonly DroneId DroneId;
        public readonly double Time;
        public readonly ControlInput Input;
        public readonly ForceCommand Command;
        public readonly DroneState State;
        public readonly AltitudeReading Altitude;
        public readonly FlightMode Mode;
        public readonly double HoldTimeRemaining;

        public FlightFrame(
            DroneId droneId,
            double time,
            ControlInput input,
            ForceCommand command,
            DroneState state,
            AltitudeReading altitude,
            FlightMode mode,
            double holdTimeRemaining)
        {
            DroneId = droneId;
            Time = time;
            Input = input;
            Command = command;
            State = state;
            Altitude = altitude;
            Mode = mode;
            HoldTimeRemaining = holdTimeRemaining;
        }
    }
}
