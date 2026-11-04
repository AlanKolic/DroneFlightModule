using DroneFlightModule.Core;
using DroneFlightModule.Pilots;

namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// One drone at the moment a decision is made: whether it is on the ground and who flies it.
    /// Pilot is null for a free drone.
    /// </summary>
    public readonly struct DroneSnapshot
    {
        public readonly DroneId Id;
        public readonly bool IsGrounded;
        public readonly PilotProfile Pilot;

        public DroneSnapshot(DroneId id, bool isGrounded, PilotProfile pilot)
        {
            Id = id;
            IsGrounded = isGrounded;
            Pilot = pilot;
        }

        public bool IsFree => Pilot == null;

        public override string ToString() => IsFree ? $"{Id} (free)" : $"{Id} ({Pilot.Id})";
    }
}
