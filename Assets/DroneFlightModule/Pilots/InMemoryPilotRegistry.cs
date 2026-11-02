using System;
using System.Collections.Generic;
using System.Linq;
using DroneFlightModule.Core;

namespace DroneFlightModule.Pilots
{
    /// <summary>
    /// A fixed roster given at construction, plus the built-in guest. Registered pilots must
    /// outrank the guest, so the guest is always strictly the lowest priority. Profiles are listed
    /// highest priority first.
    /// </summary>
    public sealed class InMemoryPilotRegistry : IPilotRegistry
    {
        private readonly Dictionary<PilotId, PilotProfile> byId = new Dictionary<PilotId, PilotProfile>();
        private readonly List<PilotProfile> profiles;

        public InMemoryPilotRegistry(IEnumerable<PilotProfile> registered)
        {
            if (registered == null)
            {
                throw new ArgumentNullException(nameof(registered));
            }

            var roster = new List<PilotProfile> { PilotProfile.Guest };
            byId.Add(PilotProfile.Guest.Id, PilotProfile.Guest);
            foreach (var profile in registered)
            {
                if (profile == null)
                {
                    throw new ArgumentException("Profiles cannot be null.", nameof(registered));
                }
                if (profile.IsGuest)
                {
                    throw new ArgumentException("The guest profile is built in and cannot be registered.", nameof(registered));
                }
                if (profile.Priority <= PilotProfile.GuestPriority)
                {
                    throw new ArgumentException($"{profile} must outrank the guest.", nameof(registered));
                }
                if (byId.ContainsKey(profile.Id))
                {
                    throw new ArgumentException($"Pilot {profile.Id} is registered twice.", nameof(registered));
                }
                byId.Add(profile.Id, profile);
                roster.Add(profile);
            }

            // OrderByDescending is stable, so equal priorities keep their roster order.
            profiles = roster.OrderByDescending(profile => profile.Priority).ToList();
        }

        public IReadOnlyList<PilotProfile> Profiles => profiles;

        public bool TryGet(PilotId id, out PilotProfile profile)
        {
            if (id.IsEmpty)
            {
                profile = null;
                return false;
            }
            return byId.TryGetValue(id, out profile);
        }
    }
}
