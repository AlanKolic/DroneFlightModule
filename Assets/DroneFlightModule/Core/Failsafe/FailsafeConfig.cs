using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// Timings and speeds of the failsafe: how long to hold, how much hold a second of flying earns
    /// back, how fast to sink and where to flare, and how quickly it reaches the velocity it targets.
    /// Speeds are positive magnitudes in m/s; the gain is in 1/s.
    /// </summary>
    public sealed class FailsafeConfig
    {
        public static readonly FailsafeConfig Default = new FailsafeConfig(
            holdDuration: 10f,
            holdRecoveryRate: 0.25f,
            descentSinkRate: 1f,
            touchdownSinkRate: 0.3f,
            flareHeight: 2f,
            fallbackSinkRate: 0.5f,
            speedGain: 4f);

        private readonly ArgumentGuard guard = new ArgumentGuard();

        public float HoldDuration { get; }
        public float HoldRecoveryRate { get; }
        public float DescentSinkRate { get; }
        public float TouchdownSinkRate { get; }
        public float FlareHeight { get; }
        public float FallbackSinkRate { get; }
        public float SpeedGain { get; }

        public FailsafeConfig(
            float holdDuration,
            float holdRecoveryRate,
            float descentSinkRate,
            float touchdownSinkRate,
            float flareHeight,
            float fallbackSinkRate,
            float speedGain)
        {
            HoldDuration = guard.RequireNonNegative(holdDuration, nameof(holdDuration));
            HoldRecoveryRate = guard.RequireNonNegative(holdRecoveryRate, nameof(holdRecoveryRate));
            DescentSinkRate = guard.RequirePositive(descentSinkRate, nameof(descentSinkRate));
            TouchdownSinkRate = guard.RequirePositive(touchdownSinkRate, nameof(touchdownSinkRate));
            FlareHeight = guard.RequirePositive(flareHeight, nameof(flareHeight));
            FallbackSinkRate = guard.RequirePositive(fallbackSinkRate, nameof(fallbackSinkRate));
            SpeedGain = guard.RequirePositive(speedGain, nameof(speedGain));
            if (holdRecoveryRate >= 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(holdRecoveryRate),
                    holdRecoveryRate,
                    "Must be below 1, or a link that is up half the time could hold the drone forever.");
            }
            if (touchdownSinkRate > descentSinkRate)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(touchdownSinkRate),
                    touchdownSinkRate,
                    "Must not exceed the descent sink rate.");
            }
        }
    }
}
