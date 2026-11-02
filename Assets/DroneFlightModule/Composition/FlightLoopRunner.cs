using System;
using DroneFlightModule.Core;
using UnityEngine;
using VContainer.Unity;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// The only place Unity's clock reaches the simulation: ticks the flight loop once per fixed
    /// update with Unity's fixed step.
    /// </summary>
    public sealed class FlightLoopRunner : IFixedTickable
    {
        private readonly FlightLoop loop;

        public FlightLoopRunner(FlightLoop loop)
        {
            this.loop = loop ?? throw new ArgumentNullException(nameof(loop));
        }

        public void FixedTick()
        {
            loop.Tick(Time.fixedDeltaTime);
        }
    }
}
