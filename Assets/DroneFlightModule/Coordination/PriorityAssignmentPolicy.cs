using System;
using System.Collections.Generic;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;

namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// Free drones first, then priority. A requested drone is assigned if free, or taken over if
    /// its pilot ranks strictly lower. Without a preference the pilot gets a free drone, grounded
    /// ones first, or else takes over the drone of the lowest-ranked pilot if they outrank them.
    /// Equal priority never takes over, and there is no queue.
    /// </summary>
    public sealed class PriorityAssignmentPolicy : IAssignmentPolicy
    {
        public AssignmentDecision Decide(PilotProfile pilot, DroneId? preferred, IReadOnlyList<DroneSnapshot> fleet)
        {
            if (pilot == null)
            {
                throw new ArgumentNullException(nameof(pilot));
            }
            if (fleet == null)
            {
                throw new ArgumentNullException(nameof(fleet));
            }
            return preferred.HasValue ? DecideFor(pilot, preferred.Value, fleet) : DecideForAny(pilot, fleet);
        }

        private AssignmentDecision DecideFor(PilotProfile pilot, DroneId preferred, IReadOnlyList<DroneSnapshot> fleet)
        {
            foreach (var drone in fleet)
            {
                if (drone.Id == preferred)
                {
                    return Claim(pilot, drone);
                }
            }
            return AssignmentDecision.Deny(FlightDenial.UnknownDrone);
        }

        private AssignmentDecision DecideForAny(PilotProfile pilot, IReadOnlyList<DroneSnapshot> fleet)
        {
            DroneSnapshot? free = null;
            DroneSnapshot? lowest = null;
            foreach (var drone in fleet)
            {
                if (drone.IsFree)
                {
                    if (free == null || (drone.IsGrounded && !free.Value.IsGrounded))
                    {
                        free = drone;
                    }
                }
                else if (lowest == null || drone.Pilot.Priority < lowest.Value.Pilot.Priority)
                {
                    lowest = drone;
                }
            }

            if (free != null)
            {
                return AssignmentDecision.Assign(free.Value.Id);
            }
            return lowest != null ? Claim(pilot, lowest.Value) : AssignmentDecision.Deny(FlightDenial.NoDroneAvailable);
        }

        private AssignmentDecision Claim(PilotProfile pilot, in DroneSnapshot drone)
        {
            if (drone.IsFree)
            {
                return AssignmentDecision.Assign(drone.Id);
            }
            if (pilot.Priority > drone.Pilot.Priority)
            {
                return AssignmentDecision.TakeOver(drone.Id);
            }
            return AssignmentDecision.Deny(FlightDenial.NoDroneAvailable);
        }
    }
}
