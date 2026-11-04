using System;
using UnityEngine;
using VContainer.Unity;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Advances the fleet's simulation clock once per fixed update with Unity's fixed step.
    /// </summary>
    public sealed class SimulationClockRunner : IFixedTickable
    {
        private readonly SimulationClock clock;

        public SimulationClockRunner(SimulationClock clock)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public void FixedTick()
        {
            clock.Advance(Time.fixedDeltaTime);
        }
    }
}
