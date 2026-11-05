using DroneFlightModule.Core;

namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// The pilot of Session stopped flying a drone, which is now free.
    /// </summary>
    public readonly struct DroneReleased
    {
        public readonly DroneId Drone;
        public readonly PilotSession Session;
        public readonly ReleaseReason Reason;

        public DroneReleased(DroneId drone, PilotSession session, ReleaseReason reason)
        {
            Drone = drone;
            Session = session;
            Reason = reason;
        }

        public PilotId Pilot => Session.PilotId;
    }
}
