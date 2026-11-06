using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// Reads through whichever source it was last told to use, so a desk can swap a device for an
    /// autopilot under a drone in flight. A new source takes effect on the next read.
    /// </summary>
    public sealed class SwitchableInputProvider : IInputProvider
    {
        private IInputProvider source;

        public SwitchableInputProvider(IInputProvider source)
        {
            Use(source);
        }

        public bool IsConnected => source.IsConnected;

        public void Use(IInputProvider source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public ControlInput Read()
        {
            return source.Read();
        }
    }
}
