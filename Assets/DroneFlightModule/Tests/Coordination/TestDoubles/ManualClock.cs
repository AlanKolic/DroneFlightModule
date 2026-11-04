namespace DroneFlightModule.Coordination.Tests
{
    /// <summary>
    /// Simulation time the test sets directly.
    /// </summary>
    internal sealed class ManualClock : ISimulationClock
    {
        public double Time { get; set; }
    }
}
