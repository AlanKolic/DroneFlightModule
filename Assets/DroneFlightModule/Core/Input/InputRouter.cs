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
        private IInputProvider source;

        public PilotId ActivePilotId { get; private set; }
        public bool IsConnected => source != null && source.IsConnected;
        public event Action<PilotId> ActiveChanged;

        public void Assign(PilotId pilotId, IInputProvider source)
        {
            if (pilotId.IsEmpty)
            {
                throw new ArgumentException("Pilot id is required.", nameof(pilotId));
            }
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (source == this)
            {
                throw new ArgumentException("A router can't route to itself.", nameof(source));
            }

            this.source = source;
            SetActivePilot(pilotId);
        }

        public void Clear()
        {
            source = null;
            SetActivePilot(default);
        }

        public ControlInput Read()
        {
            if (!IsConnected)
            {
                return ControlInput.Zero;
            }
            return source.Read();
        }

        private void SetActivePilot(PilotId pilotId)
        {
            if (pilotId.Equals(ActivePilotId))
            {
                return;
            }
            ActivePilotId = pilotId;
            ActiveChanged?.Invoke(pilotId);
        }
    }
}
