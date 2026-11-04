using DroneFlightModule.Core;

namespace DroneFlightModule.Coordination.Tests
{
    /// <summary>
    /// A pilot's input that is always connected and reads whatever the test sets.
    /// </summary>
    internal sealed class ScriptedInput : IInputProvider
    {
        public ControlInput Current { get; set; } = ControlInput.Zero;
        public bool IsConnected => true;

        public ControlInput Read() => Current;
    }
}
