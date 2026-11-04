using System;
using DroneFlightModule.Core;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// The input of one pilot at this machine. Every local pilot shares the keyboard and its link,
    /// but only the selected one steers with it; the others keep their drones with their hands off
    /// the sticks, so a parked pilot's drone holds where it is. A cut link cuts them all.
    /// </summary>
    public sealed class LocalPilotInput : IInputProvider
    {
        private readonly IInputProvider source;

        public LocalPilotInput(IInputProvider source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public bool IsSelected { get; set; }
        public bool IsConnected => source.IsConnected;

        public ControlInput Read()
        {
            return IsSelected && IsConnected ? source.Read() : ControlInput.Zero;
        }
    }
}
