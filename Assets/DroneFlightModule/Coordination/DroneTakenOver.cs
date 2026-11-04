using DroneFlightModule.Core;

namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// A higher-priority pilot took a drone over: Pilot flies it now, and FromPilot was preempted.
    /// </summary>
    public readonly struct DroneTakenOver
    {
        public readonly DroneId Drone;
        public readonly PilotId Pilot;
        public readonly PilotId FromPilot;

        public DroneTakenOver(DroneId drone, PilotId pilot, PilotId fromPilot)
        {
            Drone = drone;
            Pilot = pilot;
            FromPilot = fromPilot;
        }
    }
}
