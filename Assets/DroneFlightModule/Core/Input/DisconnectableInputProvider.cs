using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// Wraps an input source so its link can be cut on demand, for example by a debug key that
    /// simulates a dropped connection. While disconnected it reports no pilot and reads zero.
    /// </summary>
    public sealed class DisconnectableInputProvider : IInputProvider
    {
        private readonly IInputProvider source;

        public DisconnectableInputProvider(IInputProvider source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public bool IsLinkCut { get; set; }
        public bool IsConnected => !IsLinkCut && source.IsConnected;

        public ControlInput Read()
        {
            return IsConnected ? source.Read() : ControlInput.Zero;
        }
    }
}
