using DroneFlightModule.Core;

namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// A pilot started flying a free drone.
    /// </summary>
    public readonly struct DroneAssigned
    {
        public readonly DroneId Drone;
        public readonly PilotId Pilot;

        public DroneAssigned(DroneId drone, PilotId pilot)
        {
            Drone = drone;
            Pilot = pilot;
        }
    }
}
