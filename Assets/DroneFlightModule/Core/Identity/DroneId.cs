using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// Identifies one drone across the flight loop, authorization and coordination.
    /// </summary>
    public readonly struct DroneId : IEquatable<DroneId>
    {
        public readonly string Value;

        public DroneId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Drone id must be non-empty.", nameof(value));
            }
            Value = value;
        }

        public bool IsEmpty => Value == null;

        public bool Equals(DroneId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object value) => value is DroneId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public static bool operator ==(DroneId a, DroneId b) => a.Equals(b);
        public static bool operator !=(DroneId a, DroneId b) => !a.Equals(b);

        public override string ToString() => Value ?? "<none>";
    }
}
