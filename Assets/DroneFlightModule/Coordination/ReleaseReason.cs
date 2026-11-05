namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// Why a pilot stopped flying a drone, other than being taken over.
    /// </summary>
    public enum ReleaseReason
    {
        Released,
        SessionEnded,
        LinkLost,
        Revoked,
        Switched,
        Removed
    }
}
