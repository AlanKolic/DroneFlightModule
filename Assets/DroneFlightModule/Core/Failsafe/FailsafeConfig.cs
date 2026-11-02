using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// Timings and speeds of the failsafe: how long to hold, how fast to sink and where to flare.
    /// Speeds are positive magnitudes in m/s; the gain is in 1/s.
    /// </summary>
    public sealed class FailsafeConfig
    {
        public static readonly FailsafeConfig Default = new FailsafeConfig(
            holdDuration: 10f,
            descentSinkRate: 1f,
            touchdownSinkRate: 0.3f,
            flareHeight: 2f,
            fallbackSinkRate: 0.5f,
            verticalSpeedGain: 4f);

        public float HoldDuration { get; }
        public float DescentSinkRate { get; }
        public float TouchdownSinkRate { get; }
        public float FlareHeight { get; }
        public float FallbackSinkRate { get; }
        public float VerticalSpeedGain { get; }

        public FailsafeConfig(
            float holdDuration,
            float descentSinkRate,
            float touchdownSinkRate,
            float flareHeight,
            float fallbackSinkRate,
            float verticalSpeedGain)
        {
            HoldDuration = RequireNonNegative(holdDuration, nameof(holdDuration));
            DescentSinkRate = RequirePositive(descentSinkRate, nameof(descentSinkRate));
            TouchdownSinkRate = RequirePositive(touchdownSinkRate, nameof(touchdownSinkRate));
            FlareHeight = RequirePositive(flareHeight, nameof(flareHeight));
            FallbackSinkRate = RequirePositive(fallbackSinkRate, nameof(fallbackSinkRate));
            VerticalSpeedGain = RequirePositive(verticalSpeedGain, nameof(verticalSpeedGain));
            if (touchdownSinkRate > descentSinkRate)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(touchdownSinkRate),
                    touchdownSinkRate,
                    "Must not exceed the descent sink rate.");
            }
        }

        private float RequirePositive(float value, string name)
        {
            if (!(value > 0f) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(name, value, "Must be positive and finite.");
            }
            return value;
        }

        private float RequireNonNegative(float value, string name)
        {
            if (!(value >= 0f) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(name, value, "Must be non-negative and finite.");
            }
            return value;
        }
    }
}
