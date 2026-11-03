using System;
using System.Threading;
using System.Threading.Tasks;
using DroneFlightModule.Core;

namespace DroneFlightModule.Authorization
{
    /// <summary>
    /// Guest mode: grants immediately, but only to the guest pilot flying from this machine. It
    /// goes through the same request, grant and gate as any other provider; the origin rule is its
    /// own decision, not a special case elsewhere.
    /// </summary>
    public sealed class LocalGuestAuthorizationProvider : IAuthorizationProvider, IDisposable
    {
        public const string ProviderId = "local-guest";

        private readonly ArgumentGuard guard = new ArgumentGuard();
        private readonly PilotId guestPilot;
        private readonly GrantLedger grants = new GrantLedger(ProviderId);
        private bool isDisposed;

        public LocalGuestAuthorizationProvider(PilotId guestPilot)
        {
            this.guestPilot = guard.RequireId(guestPilot, nameof(guestPilot));
        }

        public string Id => ProviderId;

        public Task<AuthorizationResult> AuthorizeAsync(AuthorizationRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (isDisposed)
            {
                return Task.FromException<AuthorizationResult>(new ObjectDisposedException(nameof(LocalGuestAuthorizationProvider)));
            }
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<AuthorizationResult>(cancellationToken);
            }

            if (request.Origin != PilotOrigin.Local)
            {
                return Task.FromResult(AuthorizationResult.Denied("Guest mode is only available on the local machine."));
            }
            if (request.PilotId != guestPilot)
            {
                return Task.FromResult(AuthorizationResult.Denied($"Guest mode only authorizes the guest, not {request.PilotId}."));
            }
            return Task.FromResult(AuthorizationResult.Granted(grants.Issue(request.PilotId)));
        }

        public void Dispose()
        {
            isDisposed = true;
            grants.Dispose();
        }
    }
}
