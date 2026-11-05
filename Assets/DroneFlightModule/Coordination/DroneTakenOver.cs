using DroneFlightModule.Core;

namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// A higher-priority pilot took a drone over: Pilot flies it now, and the pilot of FromSession
    /// was preempted.
    /// </summary>
    public readonly struct DroneTakenOver
    {
        public readonly DroneId Drone;
        public readonly PilotId Pilot;
        public readonly PilotSession FromSession;

        public DroneTakenOver(DroneId drone, PilotId pilot, PilotSession fromSession)
        {
            Drone = drone;
            Pilot = pilot;
            FromSession = fromSession;
        }

        public PilotId FromPilot => FromSession.PilotId;
    }
}
