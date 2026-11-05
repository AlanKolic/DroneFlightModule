using System;
using DroneFlightModule.Coordination;
using UnityEngine;
using VContainer.Unity;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// What the fleet does once per fixed update: advances its simulation clock with Unity's fixed
    /// step, and has the coordinator release a drone that has landed with no connected pilot, so a
    /// device that never comes back does not hold its drone forever.
    /// </summary>
    public sealed class FleetRunner : IFixedTickable
    {
        private readonly SimulationClock clock;
        private readonly FlightCoordinator coordinator;

        public FleetRunner(SimulationClock clock, FlightCoordinator coordinator)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        }

        public void FixedTick()
        {
            clock.Advance(Time.fixedDeltaTime);
            coordinator.ReleaseLandedWithoutPilot();
        }
    }
}
