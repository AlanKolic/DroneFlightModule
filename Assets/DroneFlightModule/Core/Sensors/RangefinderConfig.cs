namespace DroneFlightModule.Core
{
    /// <summary>
    /// Range and rate of the rangefinder: readings above the max range (m) are invalid, and it
    /// samples this many times per second (Hz) of simulation time.
    /// </summary>
    public sealed class RangefinderConfig
    {
        public static readonly RangefinderConfig Default = new RangefinderConfig(maxRange: 10f, updateRate: 20f);

        private readonly ArgumentGuard guard = new ArgumentGuard();

        public float MaxRange { get; }
        public float UpdateRate { get; }

        public RangefinderConfig(float maxRange, float updateRate)
        {
            MaxRange = guard.RequirePositive(maxRange, nameof(maxRange));
            UpdateRate = guard.RequirePositive(updateRate, nameof(updateRate));
        }
    }
}
