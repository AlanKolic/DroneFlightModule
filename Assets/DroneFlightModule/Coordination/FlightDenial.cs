namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// Why a flight request was refused.
    /// </summary>
    public enum FlightDenial
    {
        UnknownPilot,
        NotAuthorized,
        UnknownDrone,
        NoDroneAvailable
    }
}
