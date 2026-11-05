using DroneFlightModule.Core;

namespace DroneFlightModule.Authorization
{
    /// <summary>
    /// Asks a provider whether this pilot may fly at all.
    /// </summary>
    public sealed class AuthorizationRequest
    {
        private readonly ArgumentGuard guard = new ArgumentGuard();

        public AuthorizationRequest(PilotId pilotId)
        {
            PilotId = guard.RequireId(pilotId, nameof(pilotId));
        }

        public PilotId PilotId { get; }

        public override string ToString() => PilotId.ToString();
    }
}
