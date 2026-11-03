using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DroneFlightModule.Core;

namespace DroneFlightModule.Authorization
{
    /// <summary>
    /// Stands in for a real authorization server: answers after a latency, approves or denies per
    /// pilot from either origin, and can revoke a pilot's grant at any time. The policy is read when
    /// the answer arrives, so a change during the latency counts. Disposing cancels pending
    /// requests and revokes every grant.
    /// </summary>
    public sealed class SimulatedServerAuthorizationProvider : IAuthorizationProvider, IDisposable
    {
        public const string ProviderId = "simulated-server";

        private readonly HashSet<PilotId> approved;
        private readonly TimeSpan latency;
        private readonly IDelay delay;
        private readonly GrantLedger grants = new GrantLedger(ProviderId);
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool isDisposed;

        public SimulatedServerAuthorizationProvider(IEnumerable<PilotId> approved, TimeSpan latency, IDelay delay)
        {
            if (approved == null)
            {
                throw new ArgumentNullException(nameof(approved));
            }
            if (latency < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(latency), latency, "Must not be negative.");
            }
            this.approved = new HashSet<PilotId>(approved);
            this.latency = latency;
            this.delay = delay ?? throw new ArgumentNullException(nameof(delay));
        }

        public string Id => ProviderId;

        public async Task<AuthorizationResult> AuthorizeAsync(AuthorizationRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(SimulatedServerAuthorizationProvider));
            }

            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.Token))
            {
                await delay.Wait(latency, linked.Token);
                linked.Token.ThrowIfCancellationRequested();
            }

            if (!approved.Contains(request.PilotId))
            {
                return AuthorizationResult.Denied($"The server has not approved {request.PilotId}.");
            }
            return AuthorizationResult.Granted(grants.Issue(request.PilotId));
        }

        public void Approve(PilotId pilot)
        {
            approved.Add(pilot);
        }

        public void Deny(PilotId pilot)
        {
            approved.Remove(pilot);
        }

        public bool Revoke(PilotId pilot)
        {
            return grants.Revoke(pilot);
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }
            isDisposed = true;
            lifetime.Cancel();
            grants.Dispose();
        }
    }
}
