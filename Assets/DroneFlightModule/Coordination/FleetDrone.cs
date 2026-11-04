using System;
using DroneFlightModule.Authorization;
using DroneFlightModule.Core;

namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// One drone as the coordinator sees it: its router, which decides whose input it flies on,
    /// its grant holder, which gates flight on that pilot's grant, and its status. The holder must
    /// be the one built on this router.
    /// </summary>
    public sealed class FleetDrone
    {
        private readonly ArgumentGuard guard = new ArgumentGuard();

        public FleetDrone(DroneId id, InputRouter router, DroneGrantHolder holder, IDroneStatus status)
        {
            Id = guard.RequireId(id, nameof(id));
            Router = router ?? throw new ArgumentNullException(nameof(router));
            Holder = holder ?? throw new ArgumentNullException(nameof(holder));
            Status = status ?? throw new ArgumentNullException(nameof(status));
        }

        public DroneId Id { get; }
        public InputRouter Router { get; }
        public DroneGrantHolder Holder { get; }
        public IDroneStatus Status { get; }

        public override string ToString() => Id.ToString();
    }
}
