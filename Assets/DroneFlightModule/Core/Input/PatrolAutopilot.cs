using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// A routine in place of a pilot's hands: from <see cref="Restart"/> it climbs, then flies a
    /// square of forward, right, back and left legs, round and round. It only produces stick values
    /// from the clock, knowing nothing of the drone, and it can't arm one.
    /// </summary>
    public sealed class PatrolAutopilot : IInputProvider
    {
        private const double LegSeconds = 3.0;
        private const float Stick = 0.5f;

        private static readonly Vec3[] Legs =
        {
            new Vec3(0f, 0f, Stick),
            new Vec3(Stick, 0f, 0f),
            new Vec3(0f, 0f, -Stick),
            new Vec3(-Stick, 0f, 0f)
        };

        private readonly ISimulationClock clock;
        private double startTime;

        public PatrolAutopilot(ISimulationClock clock)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public bool IsConnected => true;

        public void Restart()
        {
            startTime = clock.Time;
        }

        public ControlInput Read()
        {
            double elapsed = clock.Time - startTime;
            if (elapsed < LegSeconds)
            {
                return new ControlInput(new Vec3(0f, Stick, 0f), 0f);
            }
            int leg = (int)((elapsed - LegSeconds) / LegSeconds) % Legs.Length;
            return new ControlInput(Legs[leg], 0f);
        }
    }
}
