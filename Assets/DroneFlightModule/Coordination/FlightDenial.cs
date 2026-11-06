namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// Why a flight request or an arm was refused.
    /// </summary>
    public enum FlightDenial
    {
        UnknownPilot,
        NotAuthorized,
        PilotDisconnected,
        UnknownDrone,
        NoDroneAvailable,
        NotArmed
    }
}
