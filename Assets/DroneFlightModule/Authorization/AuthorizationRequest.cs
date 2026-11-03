using DroneFlightModule.Core;

namespace DroneFlightModule.Authorization
{
    /// <summary>
    /// Asks a provider whether this pilot, flying from this origin, may fly at all.
    /// </summary>
    public sealed class AuthorizationRequest
    {
        private readonly ArgumentGuard guard = new ArgumentGuard();

        public AuthorizationRequest(PilotId pilotId, PilotOrigin origin)
        {
            PilotId = guard.RequireId(pilotId, nameof(pilotId));
            Origin = origin;
        }

        public PilotId PilotId { get; }
        public PilotOrigin Origin { get; }

        public override string ToString() => $"{PilotId} ({Origin})";
    }
}
