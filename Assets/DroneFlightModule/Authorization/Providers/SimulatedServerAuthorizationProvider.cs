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
    /// requests and revokes every grant. Safe to call from any thread; grants are issued and
    /// revoked under one lock, so revocation callbacks run on the revoking thread while it is held.
    /// </summary>
    public sealed class SimulatedServerAuthorizationProvider : IAuthorizationProvider, IDisposable
    {
        public const string ProviderId = "simulated-server";

        private readonly HashSet<PilotId> approved;
        private readonly TimeSpan latency;
        private readonly IDelay delay;
        private readonly GrantLedger grants = new GrantLedger(ProviderId);
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private readonly object sync = new object();
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

        public Task<AuthorizationResult> AuthorizeAsync(AuthorizationRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            lock (sync)
            {
                if (isDisposed)
                {
                    return Task.FromException<AuthorizationResult>(new ObjectDisposedException(nameof(SimulatedServerAuthorizationProvider)));
                }
            }
            return AnswerAfterLatencyAsync(request.PilotId, cancellationToken);
        }

        public void Approve(PilotId pilot)
        {
            lock (sync)
            {
                approved.Add(pilot);
            }
        }

        public void Deny(PilotId pilot)
        {
            lock (sync)
            {
                approved.Remove(pilot);
            }
        }

        public bool Revoke(PilotId pilot)
        {
            lock (sync)
            {
                return grants.Revoke(pilot);
            }
        }

        public void Dispose()
        {
            lock (sync)
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

        private async Task<AuthorizationResult> AnswerAfterLatencyAsync(PilotId pilot, CancellationToken cancellationToken)
        {
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.Token))
            {
                await delay.Wait(latency, linked.Token);
            }

            // Without a synchronization context this runs on a pool thread, so the policy and ledger are read under the lock,
            // and a provider disposed while the answer was in flight issues nothing.
            lock (sync)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (isDisposed)
                {
                    throw new OperationCanceledException("The provider was disposed.", lifetime.Token);
                }
                if (!approved.Contains(pilot))
                {
                    return AuthorizationResult.Denied($"The server has not approved {pilot}.");
                }
                return AuthorizationResult.Granted(grants.Issue(pilot));
            }
        }
    }
}
