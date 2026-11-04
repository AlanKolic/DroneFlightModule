using System;
using DroneFlightModule.Core;

namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// Reads a drone's status straight from its flight loop.
    /// </summary>
    public sealed class FlightLoopStatus : IDroneStatus
    {
        private readonly FlightLoop loop;

        public FlightLoopStatus(FlightLoop loop)
        {
            this.loop = loop ?? throw new ArgumentNullException(nameof(loop));
        }

        public bool IsGrounded => loop.State.IsGrounded;
    }
}
