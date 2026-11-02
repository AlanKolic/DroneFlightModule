using System;
using DroneFlightModule.Core;

namespace DroneFlightModule.Pilots
{
    /// <summary>
    /// Who a pilot is and how much they outrank others: a higher priority takes over a drone
    /// from a lower one. The built-in guest has priority 0; registered pilots must rank above it.
    /// </summary>
    public sealed class PilotProfile
    {
        public const int GuestPriority = 0;

        public static readonly PilotProfile Guest = new PilotProfile(new PilotId("guest"), "Guest", GuestPriority);

        public PilotProfile(PilotId id, string displayName, int priority)
        {
            if (id.IsEmpty)
            {
                throw new ArgumentException("Pilot id is required.", nameof(id));
            }
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name is required.", nameof(displayName));
            }
            if (priority < GuestPriority)
            {
                throw new ArgumentOutOfRangeException(nameof(priority), priority, "Must not be below the guest priority.");
            }
            Id = id;
            DisplayName = displayName;
            Priority = priority;
        }

        public PilotId Id { get; }
        public string DisplayName { get; }
        public int Priority { get; }
        public bool IsGuest => Id == Guest.Id;

        public override string ToString() => $"{DisplayName} ({Id}, priority {Priority})";
    }
}
