using System;
using DroneFlightModule.Authorization;
using DroneFlightModule.Core;

namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// One pilot at an input device: who they are, which provider authorizes them and their input.
    /// The input belongs to the pilot rather than a drone, so the coordinator can move the session
    /// between drones without anything being registered again, and when the pilot moves to another
    /// device the session goes with them.
    /// </summary>
    public sealed class PilotSession
    {
        private readonly ArgumentGuard guard = new ArgumentGuard();

        public PilotSession(PilotId pilotId, IAuthorizationProvider provider, IInputProvider input)
        {
            PilotId = guard.RequireId(pilotId, nameof(pilotId));
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Input = input ?? throw new ArgumentNullException(nameof(input));
        }

        public PilotId PilotId { get; }
        public IAuthorizationProvider Provider { get; }
        public IInputProvider Input { get; private set; }

        public void MoveTo(IInputProvider input)
        {
            Input = input ?? throw new ArgumentNullException(nameof(input));
        }

        public override string ToString() => $"{PilotId} ({Provider.Id})";
    }
}
