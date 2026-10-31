namespace DroneFlightModule.Core
{
    /// <summary>
    /// Snapshot of one completed tick, published to observers such as telemetry.
    /// Time and State are at the end of the tick; Altitude was sampled at its start, before the step.
    /// </summary>
    public readonly struct FlightFrame
    {
        public readonly DroneId DroneId;
        public readonly double Time;
        public readonly ControlInput Input;
        public readonly ForceCommand Command;
        public readonly DroneState State;
        public readonly AltitudeReading Altitude;

        public FlightFrame(
            DroneId droneId,
            double time,
            ControlInput input,
            ForceCommand command,
            DroneState state,
            AltitudeReading altitude)
        {
            DroneId = droneId;
            Time = time;
            Input = input;
            Command = command;
            State = state;
            Altitude = altitude;
        }
    }
}
