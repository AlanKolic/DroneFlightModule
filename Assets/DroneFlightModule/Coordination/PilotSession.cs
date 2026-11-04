using System;
using DroneFlightModule.Authorization;
using DroneFlightModule.Core;

namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// One connected pilot: who they are, where they fly from, which provider authorizes them and
    /// their input. The input belongs to the pilot rather than a drone, so the coordinator can
    /// move the session between drones without anything being registered again.
    /// </summary>
    public sealed class PilotSession
    {
        private readonly ArgumentGuard guard = new ArgumentGuard();

        public PilotSession(PilotId pilotId, PilotOrigin origin, IAuthorizationProvider provider, IInputProvider input)
        {
            PilotId = guard.RequireId(pilotId, nameof(pilotId));
            Origin = origin;
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Input = input ?? throw new ArgumentNullException(nameof(input));
        }

        public PilotId PilotId { get; }
        public PilotOrigin Origin { get; }
        public IAuthorizationProvider Provider { get; }
        public IInputProvider Input { get; }

        public override string ToString() => $"{PilotId} ({Origin}, {Provider.Id})";
    }
}
