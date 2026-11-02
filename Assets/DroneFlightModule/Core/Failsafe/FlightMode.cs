namespace DroneFlightModule.Core
{
    /// <summary>
    /// Drone flight state. Holding and Descending are the failsafe after the gate closes in the air.
    /// </summary>
    public enum FlightMode
    {
        Grounded,
        Piloted,
        Holding,
        Descending
    }
}
