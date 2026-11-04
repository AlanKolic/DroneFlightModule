using System;
using DroneFlightModule.Core;
using UnityEngine;
using VContainer.Unity;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Where Unity's clock reaches a drone's simulation: ticks its flight loop once per fixed
    /// update with Unity's fixed step. Each drone scope has one.
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
