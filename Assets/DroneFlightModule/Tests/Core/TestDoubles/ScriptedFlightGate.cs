namespace DroneFlightModule.Core.Tests
{
    /// <summary>
    /// Flight gate the test opens and closes directly. Open by default.
    /// </summary>
    internal sealed class ScriptedFlightGate : IFlightGate
    {
        public bool IsFlightAllowed { get; set; } = true;
    }
}
