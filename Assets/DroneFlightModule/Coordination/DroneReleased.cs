using DroneFlightModule.Core;

namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// A pilot stopped flying a drone, which is now free.
    /// </summary>
    public readonly struct DroneReleased
    {
        public readonly DroneId Drone;
        public readonly PilotId Pilot;
        public readonly ReleaseReason Reason;

        public DroneReleased(DroneId drone, PilotId pilot, ReleaseReason reason)
        {
            Drone = drone;
            Pilot = pilot;
            Reason = reason;
        }
    }
}
