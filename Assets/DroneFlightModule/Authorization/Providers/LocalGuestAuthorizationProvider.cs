using System;
using System.Threading;
using System.Threading.Tasks;
using DroneFlightModule.Core;

namespace DroneFlightModule.Authorization
{
    /// <summary>
    /// Guest mode: grants immediately, but only to the guest pilot. It goes through the same
    /// request, grant and gate as any other provider; nothing elsewhere treats the guest specially.
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

            if (request.PilotId != guestPilot)
            {
                return Task.FromResult(new AuthorizationResult($"Guest mode only authorizes the guest, not {request.PilotId}."));
            }
            return Task.FromResult(new AuthorizationResult(grants.Issue(request.PilotId)));
        }

        public void Dispose()
        {
            isDisposed = true;
            grants.Dispose();
        }
    }
}
