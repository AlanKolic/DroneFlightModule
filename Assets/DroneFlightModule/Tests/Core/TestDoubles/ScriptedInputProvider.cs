namespace DroneFlightModule.Core.Tests
{
    /// <summary>
    /// Input provider whose output the test sets directly.
    /// </summary>
    internal sealed class ScriptedInputProvider : IInputProvider
    {
        public ControlInput Current { get; set; } = ControlInput.Zero;

        public ControlInput Read() => Current;
    }
}
