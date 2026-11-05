namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// Where a pilot stands with authorization. Granted means they hold a grant in use on a drone,
    /// so a grant left unused because no drone was available counts as Unauthorized.
    /// </summary>
    public enum AuthorizationState
    {
        Unauthorized,
        Pending,
        Granted,
        Denied,
        Revoked
    }
}
