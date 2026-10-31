namespace DroneFlightModule.Core.Tests
{
    /// <summary>
    /// Input provider whose output and connection the test sets directly.
    /// </summary>
    internal sealed class ScriptedInputProvider : IInputProvider
    {
        public ControlInput Current { get; set; } = ControlInput.Zero;
        public bool IsConnected { get; set; } = true;

        public ControlInput Read() => Current;
    }
}
