using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// One per drone. Routes the input of the pilot currently assigned to the drone, and reports
    /// who that pilot is and whether they are connected. Reads zero input when nobody is assigned
    /// or the assigned source is disconnected. Switching pilots takes effect on the next read.
    /// </summary>
    public sealed class InputRouter : IInputProvider
    {
        private readonly ArgumentGuard guard = new ArgumentGuard();
        private IInputProvider source;

        public PilotId ActivePilotId { get; private set; }
        public bool IsConnected => source != null && source.IsConnected;

        public void Assign(PilotId pilotId, IInputProvider source)
        {
            guard.RequireId(pilotId, nameof(pilotId));
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (source == this)
            {
                throw new ArgumentException("A router can't route to itself.", nameof(source));
            }

            this.source = source;
            ActivePilotId = pilotId;
        }

        public void Clear()
        {
            source = null;
            ActivePilotId = default;
        }

        public ControlInput Read()
        {
            if (!IsConnected)
            {
                return ControlInput.Zero;
            }
            return source.Read();
        }
    }
}
