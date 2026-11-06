using System.Collections.Generic;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;

namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// Chooses a drone for an authorized pilot from a snapshot of the fleet. It only decides; the
    /// coordinator carries the decision out. The fleet includes any drone the pilot already flies,
    /// and choosing that one is a fault the coordinator rejects.
    /// </summary>
    public interface IAssignmentPolicy
    {
        AssignmentDecision Decide(PilotProfile pilot, DroneId? preferred, IReadOnlyList<DroneSnapshot> fleet);
    }
}
