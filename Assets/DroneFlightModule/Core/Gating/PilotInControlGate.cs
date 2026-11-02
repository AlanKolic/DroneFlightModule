using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// Allows flight while the drone's router has an assigned input source whose pilot is still
    /// connected. A cleared router or a dropped link closes it.
    /// </summary>
    public sealed class PilotInControlGate : IFlightGate
    {
        private readonly InputRouter router;

        public PilotInControlGate(InputRouter router)
        {
            this.router = router ?? throw new ArgumentNullException(nameof(router));
        }

        public bool IsFlightAllowed => router.IsConnected;
    }
}
