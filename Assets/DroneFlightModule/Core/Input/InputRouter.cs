using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// One per drone. Routes the input of the pilot currently assigned to the drone, and reports
    /// who that pilot is and whether they are connected. Reads zero input when nobody is assigned
    /// or the assigned source is disconnected. Switching pilots takes effect on the next read.
    /// ActiveChanged reports a different pilot; SourceChanged reports every assignment or clear
    /// that changes the routed source, including a new session for the same pilot.
    /// </summary>
    public sealed class InputRouter : IInputProvider
    {
        private readonly ArgumentGuard guard = new ArgumentGuard();
        private IInputProvider source;

        public PilotId ActivePilotId { get; private set; }
        public bool IsConnected => source != null && source.IsConnected;
        public event Action<PilotId> ActiveChanged;
        public event Action<PilotId> SourceChanged;

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

            bool isNewSource = source != this.source;
            this.source = source;
            SetActivePilot(pilotId);
            if (isNewSource)
            {
                SourceChanged?.Invoke(pilotId);
            }
        }

        public void Clear()
        {
            bool hadSource = source != null;
            source = null;
            SetActivePilot(default);
            if (hadSource)
            {
                SourceChanged?.Invoke(default);
            }
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
