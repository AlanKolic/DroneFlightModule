namespace DroneFlightModule.Core
{
    /// <summary>
    /// Measures height above ground from the true state, as a real sensor would see it: possibly
    /// stale, out of range or invalid. The failsafe flares on this reading, not on the true height.
    /// </summary>
    public interface IAltitudeSensor
    {
        AltitudeReading Read(in DroneState truth, double time);
    }
}
