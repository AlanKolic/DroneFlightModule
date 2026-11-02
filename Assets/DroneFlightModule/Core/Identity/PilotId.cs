using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// Identifies one pilot across input routing, authorization and coordination.
    /// </summary>
    public readonly struct PilotId : IEquatable<PilotId>
    {
        public readonly string Value;

        public PilotId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Pilot id must be non-empty.", nameof(value));
            }
            Value = value;
        }

        public bool IsEmpty => Value == null;

        public bool Equals(PilotId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object value) => value is PilotId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public static bool operator ==(PilotId left, PilotId right) => left.Equals(right);
        public static bool operator !=(PilotId left, PilotId right) => !left.Equals(right);

        public override string ToString() => Value ?? "<none>";
    }
}
