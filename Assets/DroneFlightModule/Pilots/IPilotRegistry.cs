using System.Collections.Generic;
using DroneFlightModule.Core;

namespace DroneFlightModule.Pilots
{
    /// <summary>
    /// The pilots allowed to fly at all, and their priorities. A pilot it doesn't know is denied
    /// before any authorization provider is asked. The guest profile is always present.
    /// </summary>
    public interface IPilotRegistry
    {
        IReadOnlyList<PilotProfile> Profiles { get; }

        bool TryGet(PilotId id, out PilotProfile profile);
    }
}
