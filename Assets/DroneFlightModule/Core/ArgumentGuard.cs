using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// Shared argument checks: numeric settings, where NaN and infinity always fail so a bad config
    /// is rejected where it is built, and required pilot and drone ids. Each check returns the value.
    /// </summary>
    public sealed class ArgumentGuard
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

        public PilotId RequireId(PilotId id, string name)
        {
            if (id.IsEmpty)
            {
                throw new ArgumentException("Pilot id is required.", name);
            }
            return id;
        }

        public DroneId RequireId(DroneId id, string name)
        {
            if (id.IsEmpty)
            {
                throw new ArgumentException("Drone id is required.", name);
            }
            return id;
        }
    }
}
