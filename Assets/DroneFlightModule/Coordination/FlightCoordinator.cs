using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DroneFlightModule.Authorization;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;

namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// Decides which drone each pilot session flies. A request looks the pilot up, asks the
    /// session's provider to authorize them, asks the policy for a decision on a fresh snapshot of
    /// the fleet, then carries it out in one step between ticks: the grant is installed and the
    /// router switched together, so a takeover never closes the gate. A pilot who already flies a
    /// drone and is given another lets go of the first only then, so a denied request leaves them
    /// where they were. Asking for the drone they already fly just routes it to this session. A
    /// new request from a session cancels its pending one. A drone is freed when its pilot
    /// releases it, the session ends, or the provider revokes the grant.
    /// </summary>
    public sealed class FlightCoordinator
    {
        private readonly IPilotRegistry pilots;
        private readonly DroneRegistry drones;
        private readonly IAssignmentPolicy policy;
        private readonly Dictionary<DroneId, Assignment> assignments = new Dictionary<DroneId, Assignment>();
        private readonly Dictionary<PilotSession, CancellationTokenSource> pending = new Dictionary<PilotSession, CancellationTokenSource>();

        public FlightCoordinator(IPilotRegistry pilots, DroneRegistry drones, IAssignmentPolicy policy)
        {
            this.pilots = pilots ?? throw new ArgumentNullException(nameof(pilots));
            this.drones = drones ?? throw new ArgumentNullException(nameof(drones));
            this.policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        public event Action<DroneAssigned> Assigned;
        public event Action<DroneReleased> Released;
        public event Action<DroneTakenOver> TookOver;

        public bool TryGetDrone(PilotId pilot, out DroneId drone)
        {
            foreach (var assignment in assignments.Values)
            {
                if (assignment.Session.PilotId == pilot)
                {
                    drone = assignment.Drone.Id;
                    return true;
                }
            }
            drone = default;
            return false;
        }

        public bool TryGetPilot(DroneId drone, out PilotId pilot)
        {
            if (assignments.TryGetValue(drone, out var assignment))
            {
                pilot = assignment.Session.PilotId;
                return true;
            }
            pilot = default;
            return false;
        }

        public Task<FlightRequestResult> RequestFlightAsync(PilotSession session, DroneId? preferred, CancellationToken cancellationToken)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }
            if (preferred.HasValue && preferred.Value.IsEmpty)
            {
                throw new ArgumentException("A preferred drone needs an id; pass null for any drone.", nameof(preferred));
            }
            return RequestAsync(session, preferred, cancellationToken);
        }

        public bool Release(PilotSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }
            return ReleaseFlownBy(session, ReleaseReason.Released);
        }

        public void EndSession(PilotSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }
            CancelPending(session);
            ReleaseFlownBy(session, ReleaseReason.SessionEnded);
        }

        private async Task<FlightRequestResult> RequestAsync(PilotSession session, DroneId? preferred, CancellationToken cancellationToken)
        {
            CancelPending(session);
            var request = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            // Read once: a newer request disposes this source when it cancels it.
            var token = request.Token;
            pending.Add(session, request);
            try
            {
                if (!pilots.TryGet(session.PilotId, out var profile))
                {
                    return FlightRequestResult.Denied(FlightDenial.UnknownPilot, $"{session.PilotId} is not a registered pilot.");
                }

                var authorization = await session.Provider.AuthorizeAsync(new AuthorizationRequest(session.PilotId, session.Origin), token);
                token.ThrowIfCancellationRequested();
                if (!authorization.IsGranted)
                {
                    return FlightRequestResult.Denied(FlightDenial.NotAuthorized, authorization.DenialReason);
                }
                if (authorization.Grant.IsRevoked)
                {
                    return FlightRequestResult.Denied(FlightDenial.NotAuthorized, $"{session.PilotId}'s grant was revoked before it was used.");
                }
                return CarryOut(session, profile, authorization.Grant, preferred);
            }
            finally
            {
                // Whoever takes the source out of the pending list disposes it.
                if (pending.TryGetValue(session, out var current) && current == request)
                {
                    pending.Remove(session);
                    request.Dispose();
                }
            }
        }

        private FlightRequestResult CarryOut(PilotSession session, PilotProfile profile, AuthorizationGrant grant, DroneId? preferred)
        {
            DroneId? current = TryGetDrone(session.PilotId, out var flown) ? flown : (DroneId?)null;
            if (current.HasValue && preferred == current)
            {
                Route(assignments[current.Value].Drone, session, profile, grant);
                return FlightRequestResult.Assigned(current.Value);
            }

            var decision = policy.Decide(profile, preferred, Snapshot(current));
            if (decision.Action == AssignmentAction.Deny)
            {
                return FlightRequestResult.Denied(decision.Denial, DenialMessage(decision.Denial, session.PilotId, preferred));
            }
            var target = Validate(decision, current);

            // The new drone is secured, so only now does the pilot let go of the one they fly.
            if (current.HasValue)
            {
                ReleaseDrone(current.Value, ReleaseReason.Switched);
            }
            if (decision.Action == AssignmentAction.TakeOver)
            {
                var fromPilot = assignments[target.Id].Session.PilotId;
                Route(target, session, profile, grant);
                TookOver?.Invoke(new DroneTakenOver(target.Id, session.PilotId, fromPilot));
                return FlightRequestResult.TookOver(target.Id, fromPilot);
            }
            Route(target, session, profile, grant);
            Assigned?.Invoke(new DroneAssigned(target.Id, session.PilotId));
            return FlightRequestResult.Assigned(target.Id);
        }

        private List<DroneSnapshot> Snapshot(DroneId? excluded)
        {
            var fleet = new List<DroneSnapshot>(drones.Drones.Count);
            foreach (var drone in drones.Drones)
            {
                if (drone.Id == excluded)
                {
                    continue;
                }
                var pilot = assignments.TryGetValue(drone.Id, out var assignment) ? assignment.Profile : null;
                fleet.Add(new DroneSnapshot(drone.Id, drone.Status.IsGrounded, pilot));
            }
            return fleet;
        }

        // Checked before anything changes, so a faulty policy can't leave a pilot half switched.
        private FleetDrone Validate(in AssignmentDecision decision, DroneId? current)
        {
            if (!drones.TryGet(decision.Drone, out var target) || decision.Drone == current)
            {
                throw new InvalidOperationException($"The policy chose {decision.Drone}, which it was not offered.");
            }
            bool isOccupied = assignments.ContainsKey(target.Id);
            if (decision.Action == AssignmentAction.TakeOver && !isOccupied)
            {
                throw new InvalidOperationException($"The policy took over {target.Id}, which is free.");
            }
            if (decision.Action == AssignmentAction.Assign && isOccupied)
            {
                throw new InvalidOperationException($"The policy assigned {target.Id}, which is taken.");
            }
            return target;
        }

        private void Route(FleetDrone drone, PilotSession session, PilotProfile profile, AuthorizationGrant grant)
        {
            if (assignments.TryGetValue(drone.Id, out var replaced))
            {
                replaced.Revocation.Dispose();
            }
            drone.Holder.Install(grant);
            drone.Router.Assign(session.PilotId, session.Input);
            var revocation = grant.Revoked.Register(() => ReleaseDrone(drone.Id, ReleaseReason.Revoked));
            assignments[drone.Id] = new Assignment(drone, session, profile, revocation);
        }

        private bool ReleaseFlownBy(PilotSession session, ReleaseReason reason)
        {
            foreach (var assignment in assignments.Values)
            {
                if (assignment.Session == session)
                {
                    return ReleaseDrone(assignment.Drone.Id, reason);
                }
            }
            return false;
        }

        private bool ReleaseDrone(DroneId droneId, ReleaseReason reason)
        {
            if (!assignments.TryGetValue(droneId, out var assignment))
            {
                return false;
            }
            assignments.Remove(droneId);
            assignment.Revocation.Dispose();
            assignment.Drone.Holder.Clear();
            assignment.Drone.Router.Clear();
            Released?.Invoke(new DroneReleased(droneId, assignment.Session.PilotId, reason));
            return true;
        }

        private void CancelPending(PilotSession session)
        {
            if (!pending.TryGetValue(session, out var previous))
            {
                return;
            }
            pending.Remove(session);
            previous.Cancel();
            previous.Dispose();
        }

        private string DenialMessage(FlightDenial denial, PilotId pilot, DroneId? preferred)
        {
            switch (denial)
            {
                case FlightDenial.UnknownDrone:
                    return $"{preferred} is not in the fleet.";
                case FlightDenial.NoDroneAvailable:
                    return preferred.HasValue
                        ? $"{preferred} is flown by a pilot {pilot} does not outrank."
                        : $"No drone is free, and {pilot} outranks no pilot flying one.";
                default:
                    return $"{pilot} was denied: {denial}.";
            }
        }

        private sealed class Assignment
        {
            public Assignment(FleetDrone drone, PilotSession session, PilotProfile profile, CancellationTokenRegistration revocation)
            {
                Drone = drone;
                Session = session;
                Profile = profile;
                Revocation = revocation;
            }

            public FleetDrone Drone { get; }
            public PilotSession Session { get; }
            public PilotProfile Profile { get; }
            public CancellationTokenRegistration Revocation { get; }
        }
    }
}
