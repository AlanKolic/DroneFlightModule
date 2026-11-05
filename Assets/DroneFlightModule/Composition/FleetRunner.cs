using System;
using DroneFlightModule.Coordination;
using UnityEngine;
using VContainer.Unity;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// What the fleet does once per fixed update: advances its simulation clock with Unity's fixed
    /// step, has the coordinator release a drone that has landed with no connected pilot, and
    /// removes the stations of devices that are gone and hold no drone, so a device that never
    /// comes back leaves nothing behind.
    /// </summary>
    public sealed class FleetRunner : IFixedTickable
    {
        private readonly SimulationClock clock;
        private readonly FlightCoordinator coordinator;
        private readonly InputDeviceStations stations;

        public FleetRunner(SimulationClock clock, FlightCoordinator coordinator, InputDeviceStations stations)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            this.stations = stations ?? throw new ArgumentNullException(nameof(stations));
        }

        public void FixedTick()
        {
            clock.Advance(Time.fixedDeltaTime);
            coordinator.ReleaseLandedWithoutPilot();
            stations.RemoveUnattended();
        }
    }
}
