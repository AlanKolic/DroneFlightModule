using System;
using System.Threading;
using DroneFlightModule.Core;

namespace DroneFlightModule.Authorization
{
    /// <summary>
    /// One per drone. Holds the grant of the pilot assigned to the drone and opens the gate only
    /// while that grant is unrevoked and belongs to the router's active pilot, so a grant for one
    /// pilot never lets another fly. Installing claims the drone for the pilot in the shared
    /// <see cref="PilotClaims"/>, so a pilot who controls another drone is refused; clearing,
    /// replacing or revoking the grant releases the claim. Installing a grant and switching the
    /// router happen together, between ticks, so a takeover never sees the gate closed.
    /// A revocation also empties <see cref="Grant"/>.
    /// </summary>
    public sealed class DroneGrantHolder : IFlightGate
    {
        private readonly ArgumentGuard guard = new ArgumentGuard();
        private readonly DroneId drone;
        private readonly InputRouter router;
        private readonly PilotClaims claims;
        private CancellationTokenRegistration revocation;

        public DroneGrantHolder(DroneId drone, InputRouter router, PilotClaims claims)
        {
            this.drone = guard.RequireId(drone, nameof(drone));
            this.router = router ?? throw new ArgumentNullException(nameof(router));
            this.claims = claims ?? throw new ArgumentNullException(nameof(claims));
        }

        public AuthorizationGrant Grant { get; private set; }

        public bool IsFlightAllowed => Grant != null && !Grant.IsRevoked && Grant.PilotId == router.ActivePilotId;

        public void Install(AuthorizationGrant grant)
        {
            if (grant == null)
            {
                throw new ArgumentNullException(nameof(grant));
            }
            if (grant.IsRevoked)
            {
                throw new ArgumentException("The grant is already revoked.", nameof(grant));
            }

            // Claim first: it refuses a pilot who controls another drone before anything here changes.
            claims.Claim(grant.PilotId, drone);
            revocation.Dispose();
            if (Grant != null && Grant.PilotId != grant.PilotId)
            {
                claims.Release(Grant.PilotId, drone);
            }
            Grant = grant;
            revocation = grant.Revoked.Register(() => OnRevoked(grant));
        }

        public void Clear()
        {
            revocation.Dispose();
            revocation = default;
            if (Grant != null)
            {
                claims.Release(Grant.PilotId, drone);
            }
            Grant = null;
        }

        private void OnRevoked(AuthorizationGrant revoked)
        {
            claims.Release(revoked.PilotId, drone);
            Grant = null;
        }
    }
}
