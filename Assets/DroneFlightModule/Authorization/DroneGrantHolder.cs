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
    /// </summary>
    public sealed class DroneGrantHolder : IFlightGate
    {
        private readonly DroneId drone;
        private readonly InputRouter router;
        private readonly PilotClaims claims;
        private CancellationTokenRegistration revocation;

        public DroneGrantHolder(DroneId drone, InputRouter router, PilotClaims claims)
        {
            if (drone.IsEmpty)
            {
                throw new ArgumentException("Drone id is required.", nameof(drone));
            }
            this.drone = drone;
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
            if (claims.TryGetDrone(grant.PilotId, out var claimed) && claimed != drone)
            {
                throw new InvalidOperationException($"{grant.PilotId} already controls {claimed}.");
            }

            ReleaseCurrent();
            claims.Claim(grant.PilotId, drone);
            Grant = grant;
            revocation = grant.Revoked.Register(() => claims.Release(grant.PilotId, drone));
        }

        public void Clear()
        {
            ReleaseCurrent();
            Grant = null;
        }

        private void ReleaseCurrent()
        {
            revocation.Dispose();
            revocation = default;
            if (Grant != null)
            {
                claims.Release(Grant.PilotId, drone);
            }
        }
    }
}
