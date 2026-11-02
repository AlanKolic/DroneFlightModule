using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// Shared constructor checks for numeric settings. NaN and infinity always fail, so a bad
    /// config is rejected where it is built instead of corrupting the simulation later.
    /// </summary>
    internal sealed class ArgumentGuard
    {
        public float RequirePositive(float value, string name)
        {
            if (!(value > 0f) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(name, value, "Must be positive and finite.");
            }
            return value;
        }

        public float RequireNonNegative(float value, string name)
        {
            if (!(value >= 0f) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(name, value, "Must be non-negative and finite.");
            }
            return value;
        }
    }
}
