namespace DroneFlightModule.Core
{
    /// <summary>
    /// Drone flight state. Disarmed and Armed are on the ground: only Armed takes the pilot's
    /// command. Holding and Descending are the failsafe after the gate closes in the air.
    /// </summary>
    public enum FlightMode
    {
        Disarmed,
        Armed,
        Piloted,
        Holding,
        Descending
    }
}
