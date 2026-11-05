using System;
using DroneFlightModule.Coordination;
using VContainer.Unity;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Once per fixed update, has the coordinator release a drone that has landed with no connected
    /// pilot, so a device that never comes back does not hold its drone forever.
    /// </summary>
    public sealed class UnattendedDroneRunner : IFixedTickable
    {
        private readonly FlightCoordinator coordinator;

        public UnattendedDroneRunner(FlightCoordinator coordinator)
        {
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        }

        public void FixedTick()
        {
            coordinator.ReleaseLandedWithoutPilot();
        }
    }
}
