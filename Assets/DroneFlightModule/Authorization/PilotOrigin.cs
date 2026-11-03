namespace DroneFlightModule.Authorization
{
    /// <summary>
    /// Where a pilot is flying from: this machine (keyboard, gamepad) or over the network (phone).
    /// </summary>
    public enum PilotOrigin
    {
        Local,
        Remote
    }
}
