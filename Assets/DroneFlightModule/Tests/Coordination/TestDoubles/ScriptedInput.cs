using DroneFlightModule.Core;

namespace DroneFlightModule.Coordination.Tests
{
    /// <summary>
    /// A pilot's input that reads whatever the test sets, connected until the test says otherwise.
    /// </summary>
    internal sealed class ScriptedInput : IInputProvider
    {
        public ControlInput Current { get; set; } = ControlInput.Zero;
        public bool IsConnected { get; set; } = true;

        public ControlInput Read() => Current;
    }
}
