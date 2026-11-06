namespace DroneFlightModule.Core
{
    /// <summary>
    /// The simulation's current time in seconds, shared by the whole fleet.
    /// </summary>
    public interface ISimulationClock
    {
        double Time { get; }
    }
}
