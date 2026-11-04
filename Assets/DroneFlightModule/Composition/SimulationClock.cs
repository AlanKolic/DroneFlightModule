using DroneFlightModule.Coordination;
using DroneFlightModule.Core;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// The fleet's simulation time, advanced by the same fixed step as every drone's loop.
    /// </summary>
    public sealed class SimulationClock : ISimulationClock
    {
        private readonly ArgumentGuard guard = new ArgumentGuard();

        public double Time { get; private set; }

        public void Advance(float deltaTime)
        {
            Time += guard.RequirePositive(deltaTime, nameof(deltaTime));
        }
    }
}
