using System.Collections.Generic;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;

namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// Chooses a drone for an authorized pilot from a snapshot of the fleet. It only decides; the
    /// coordinator carries the decision out. The fleet never includes the pilot's own drone.
    /// </summary>
    public interface IAssignmentPolicy
    {
        AssignmentDecision Decide(PilotProfile pilot, DroneId? preferred, IReadOnlyList<DroneSnapshot> fleet);
    }
}
