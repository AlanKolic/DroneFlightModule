using System;
using System.Collections.Generic;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;
using UnityEngine;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// The registered pilots, mapped to <see cref="PilotProfile"/>s, which validate them; the
    /// pilot registry adds the built-in guest and checks the roster as a whole. Every registered
    /// pilot signs in through the simulated server.
    /// </summary>
    [CreateAssetMenu(menuName = "DroneFlightModule/Pilot Roster", fileName = "PilotRoster")]
    public sealed class PilotRosterAsset : ScriptableObject
    {
        [SerializeField] private PilotEntry[] pilots = { new PilotEntry("ace", "Ace", 50) };

        public IReadOnlyList<PilotProfile> ToProfiles()
        {
            var profiles = new PilotProfile[pilots.Length];
            for (int i = 0; i < pilots.Length; i++)
            {
                profiles[i] = new PilotProfile(new PilotId(pilots[i].Id), pilots[i].DisplayName, pilots[i].Priority);
            }
            return profiles;
        }

        [Serializable]
        private sealed class PilotEntry
        {
            [SerializeField] private string id;
            [SerializeField] private string displayName;
            [SerializeField] private int priority;

            public PilotEntry(string id, string displayName, int priority)
            {
                this.id = id;
                this.displayName = displayName;
                this.priority = priority;
            }

            public string Id => id;
            public string DisplayName => displayName;
            public int Priority => priority;
        }
    }
}
