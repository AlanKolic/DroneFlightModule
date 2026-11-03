using System;
using System.Collections.Generic;
using System.Threading;
using DroneFlightModule.Core;

namespace DroneFlightModule.Authorization
{
    /// <summary>
    /// The grants one provider has issued: at most one live grant per pilot, and a repeat request
    /// returns it. Revoking cancels the grant's token, which runs its registered callbacks on the
    /// revoking thread.
    /// </summary>
    public sealed class GrantLedger : IDisposable
    {
        private readonly ArgumentGuard guard = new ArgumentGuard();
        private readonly string providerId;
        private readonly Dictionary<PilotId, (CancellationTokenSource Source, AuthorizationGrant Grant)> live =
            new Dictionary<PilotId, (CancellationTokenSource Source, AuthorizationGrant Grant)>();

        public GrantLedger(string providerId)
        {
            if (string.IsNullOrWhiteSpace(providerId))
            {
                throw new ArgumentException("Provider id is required.", nameof(providerId));
            }
            this.providerId = providerId;
        }

        public AuthorizationGrant Issue(PilotId pilot)
        {
            guard.RequireId(pilot, nameof(pilot));
            if (live.TryGetValue(pilot, out var existing))
            {
                return existing.Grant;
            }

            var source = new CancellationTokenSource();
            var grant = new AuthorizationGrant(pilot, providerId, source.Token);
            live.Add(pilot, (source, grant));
            return grant;
        }

        public bool Revoke(PilotId pilot)
        {
            if (!live.TryGetValue(pilot, out var entry))
            {
                return false;
            }
            live.Remove(pilot);
            // Not disposed: holders may still register on the revoked token, and a plain source owns nothing unmanaged.
            entry.Source.Cancel();
            return true;
        }

        public void RevokeAll()
        {
            var pilots = new List<PilotId>(live.Keys);
            foreach (var pilot in pilots)
            {
                Revoke(pilot);
            }
        }

        public void Dispose()
        {
            RevokeAll();
        }
    }
}
