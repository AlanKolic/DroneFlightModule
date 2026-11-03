using System;
using System.Threading;
using DroneFlightModule.Core;

namespace DroneFlightModule.Authorization
{
    /// <summary>
    /// Permission for one pilot to fly, issued by one provider. The provider can revoke it at any
    /// time by cancelling <see cref="Revoked"/>; anyone holding the grant can check or register on it.
    /// </summary>
    public sealed class AuthorizationGrant
    {
        public AuthorizationGrant(PilotId pilotId, string providerId, CancellationToken revoked)
        {
            if (pilotId.IsEmpty)
            {
                throw new ArgumentException("Pilot id is required.", nameof(pilotId));
            }
            if (string.IsNullOrWhiteSpace(providerId))
            {
                throw new ArgumentException("Provider id is required.", nameof(providerId));
            }
            PilotId = pilotId;
            ProviderId = providerId;
            Revoked = revoked;
        }

        public PilotId PilotId { get; }
        public string ProviderId { get; }
        public CancellationToken Revoked { get; }
        public bool IsRevoked => Revoked.IsCancellationRequested;

        public override string ToString() => $"{PilotId} by {ProviderId}{(IsRevoked ? " (revoked)" : string.Empty)}";
    }
}
