using DroneFlightModule.Core;

namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// A pilot moved their drone to another of their sessions, such as another device. The flight
    /// goes on; FromSession just no longer steers it.
    /// </summary>
    public readonly struct DroneMoved
    {
        public readonly DroneId Drone;
        public readonly PilotSession FromSession;

        public DroneMoved(DroneId drone, PilotSession fromSession)
        {
            Drone = drone;
            FromSession = fromSession;
        }

        public PilotId Pilot => FromSession.PilotId;
    }
}
