using System;
using DroneFlightModule.Core;

namespace DroneFlightModule.Authorization
{
    /// <summary>
    /// Asks a provider whether this pilot, flying from this origin, may fly at all.
    /// </summary>
    public sealed class AuthorizationRequest
    {
        public AuthorizationRequest(PilotId pilotId, PilotOrigin origin)
        {
            if (pilotId.IsEmpty)
            {
                throw new ArgumentException("Pilot id is required.", nameof(pilotId));
            }
            PilotId = pilotId;
            Origin = origin;
        }

        public PilotId PilotId { get; }
        public PilotOrigin Origin { get; }

        public override string ToString() => $"{PilotId} ({Origin})";
    }
}
