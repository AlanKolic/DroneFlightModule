using System;
using DroneFlightModule.Core;

namespace DroneFlightModule.Authorization
{
    /// <summary>
    /// One per drone. Holds the grant of the pilot assigned to the drone and opens the gate only
    /// while that grant is unrevoked and belongs to the router's active pilot, so a grant for one
    /// pilot never lets another fly. It only gates: who flies which drone is the coordinator's to
    /// decide. Installing a grant and switching the router happen together, between ticks, so a
    /// takeover never sees the gate closed.
    /// </summary>
    public sealed class DroneGrantHolder : IFlightGate
    {
        private readonly InputRouter router;

        public DroneGrantHolder(InputRouter router)
        {
            this.router = router ?? throw new ArgumentNullException(nameof(router));
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
            Grant = grant;
        }

        public void Clear()
        {
            Grant = null;
        }
    }
}
