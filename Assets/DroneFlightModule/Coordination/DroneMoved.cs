using DroneFlightModule.Core;

namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// A pilot moved to another input device, and their session and its drone went with them. The
    /// flight goes on.
    /// </summary>
    public readonly struct DroneMoved
    {
        public readonly DroneId Drone;
        public readonly PilotSession Session;

        public DroneMoved(DroneId drone, PilotSession session)
        {
            Drone = drone;
            Session = session;
        }

        public PilotId Pilot => Session.PilotId;
    }
}
