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
    /// Decides which drone each pilot flies. A pilot flies at most one drone, whichever session
    /// they use. A request authorizes the session through its provider, asks the policy on a fresh
    /// snapshot of the fleet, then carries the decision out in one step between ticks: the grant
    /// and the route change together, so a takeover never closes the gate. A session whose input is
    /// not connected when the answer arrives is refused, so a drone never goes to a pilot who
    /// cannot fly it. A drone that has landed with no connected pilot is released, so a lost
    /// device does not hold it forever. It keeps each pilot's <see cref="AuthorizationState"/> for
    /// display; a pilot flying a drone stays Granted whatever becomes of a further request. Each
    /// change of who flies what, and each denial, is raised as an event once the change is complete;
    /// a listener that throws is reported through ListenerFailed and cannot undo or interrupt it.
    /// </summary>
    public sealed class FlightCoordinator
    {
        private readonly IPilotRegistry pilots;
        private readonly DroneRegistry drones;
        private readonly IAssignmentPolicy policy;
        private readonly Dictionary<DroneId, Assignment> assignments = new Dictionary<DroneId, Assignment>();
        private readonly Dictionary<PilotSession, CancellationTokenSource> pending = new Dictionary<PilotSession, CancellationTokenSource>();
        private readonly Dictionary<PilotId, AuthorizationState> authorizations = new Dictionary<PilotId, AuthorizationState>();

        public FlightCoordinator(IPilotRegistry pilots, DroneRegistry drones, IAssignmentPolicy policy)
        {
            this.pilots = pilots ?? throw new ArgumentNullException(nameof(pilots));
            this.drones = drones ?? throw new ArgumentNullException(nameof(drones));
            this.policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        public event Action<DroneAssigned> Assigned;
        public event Action<DroneReleased> Released;
        public event Action<DroneTakenOver> TookOver;
        public event Action<DroneMoved> Moved;
        public event Action<FlightDenied> Denied;
        public event Action<Exception> ListenerFailed;

        public AuthorizationState AuthorizationOf(PilotId pilot)
        {
            return authorizations.TryGetValue(pilot, out var state) ? state : AuthorizationState.Unauthorized;
        }

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
            CancelPending(session);
            return TryGetDrone(session, out var drone) && ReleaseDrone(drone, ReleaseReason.Released);
        }

        public void EndSession(PilotSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }
            CancelPending(session);
            if (TryGetDrone(session, out var drone))
            {
                ReleaseDrone(drone, ReleaseReason.SessionEnded);
            }
            authorizations.Remove(session.PilotId);
        }

        // For a session whose input moved: its drone follows, with the grant and the flight untouched.
        public void Reroute(PilotSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }
            if (TryGetDrone(session, out var drone))
            {
                assignments[drone].Drone.Router.Assign(session.PilotId, session.Input);
                Raise(Moved, new DroneMoved(drone, session));
            }
        }

        // At most one per call, which allocates nothing; called every tick, the next drone goes on the next tick.
        public void ReleaseLandedWithoutPilot()
        {
            foreach (var assignment in assignments.Values)
            {
                if (assignment.Drone.Status.IsGrounded && !assignment.Session.Input.IsConnected)
                {
                    ReleaseDrone(assignment.Drone.Id, ReleaseReason.LinkLost);
                    return;
                }
            }
        }

        // Released first, so its pilot's flight ends before the drone is gone; a pending request for it is then denied as UnknownDrone.
        public bool RemoveDrone(DroneId drone)
        {
            ReleaseDrone(drone, ReleaseReason.Removed);
            return drones.Remove(drone);
        }

        // The request ends with an OperationCanceledException and raises no event.
        public bool CancelRequest(PilotSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }
            return CancelPending(session);
        }

        private bool TryGetDrone(PilotSession session, out DroneId drone)
        {
            foreach (var assignment in assignments.Values)
            {
                if (assignment.Session == session)
                {
                    drone = assignment.Drone.Id;
                    return true;
                }
            }
            drone = default;
            return false;
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
                    return Deny(session, preferred, FlightDenial.UnknownPilot, $"{session.PilotId} is not a registered pilot.");
                }

                Settle(session.PilotId, AuthorizationState.Pending);
                var authorization = await session.Provider.AuthorizeAsync(new AuthorizationRequest(session.PilotId), token);
                token.ThrowIfCancellationRequested();
                if (!authorization.IsGranted)
                {
                    Settle(session.PilotId, AuthorizationState.Denied);
                    return Deny(session, preferred, FlightDenial.NotAuthorized, authorization.DenialReason);
                }
                if (authorization.Grant.IsRevoked)
                {
                    Settle(session.PilotId, AuthorizationState.Revoked);
                    return Deny(session, preferred, FlightDenial.NotAuthorized, $"{session.PilotId}'s grant was revoked before it was used.");
                }
                // Checked after the wait, because the link can drop while the provider answers.
                if (!session.Input.IsConnected)
                {
                    return Deny(session, preferred, FlightDenial.PilotDisconnected, $"{session.PilotId} is not connected.");
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
                    EndPending(session.PilotId);
                }
            }
        }

        private FlightRequestResult CarryOut(PilotSession session, PilotProfile profile, AuthorizationGrant grant, DroneId? preferred)
        {
            DroneId? current = TryGetDrone(session.PilotId, out var flown) ? flown : (DroneId?)null;
            // Any drone will do for a pilot who already flies one, so they keep it.
            if (current.HasValue && (!preferred.HasValue || preferred == current))
            {
                Route(assignments[current.Value].Drone, session, profile, grant);
                return new FlightRequestResult(current.Value);
            }

            var decision = policy.Decide(profile, preferred, Snapshot());
            if (decision.Action == AssignmentAction.Deny)
            {
                return Deny(session, preferred, decision.Denial, DenialMessage(decision.Denial, session.PilotId, preferred));
            }
            var target = Validate(decision, current);

            // The new drone is secured, so only now does the pilot let go of the one they fly.
            var left = current.HasValue ? Free(current.Value) : null;
            var fromSession = decision.Action == AssignmentAction.TakeOver ? assignments[target.Id].Session : null;
            Route(target, session, profile, grant);

            if (left != null)
            {
                Raise(Released, new DroneReleased(left.Drone.Id, left.Session, ReleaseReason.Switched));
            }
            if (decision.Action == AssignmentAction.TakeOver)
            {
                LoseGrant(fromSession, AuthorizationState.Unauthorized);
                Raise(TookOver, new DroneTakenOver(target.Id, session.PilotId, fromSession));
                return new FlightRequestResult(target.Id, fromSession.PilotId);
            }
            Raise(Assigned, new DroneAssigned(target.Id, session.PilotId));
            return new FlightRequestResult(target.Id);
        }

        private List<DroneSnapshot> Snapshot()
        {
            var fleet = new List<DroneSnapshot>(drones.Drones.Count);
            foreach (var drone in drones.Drones)
            {
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
            authorizations[session.PilotId] = AuthorizationState.Granted;
        }

        private bool ReleaseDrone(DroneId droneId, ReleaseReason reason)
        {
            var freed = Free(droneId);
            if (freed == null)
            {
                return false;
            }
            LoseGrant(freed.Session, reason == ReleaseReason.Revoked ? AuthorizationState.Revoked : AuthorizationState.Unauthorized);
            Raise(Released, new DroneReleased(droneId, freed.Session, reason));
            return true;
        }

        // Changes state only; the caller raises the event once everything has changed.
        private Assignment Free(DroneId droneId)
        {
            if (!assignments.TryGetValue(droneId, out var assignment))
            {
                return null;
            }
            assignments.Remove(droneId);
            assignment.Revocation.Dispose();
            assignment.Drone.Holder.Clear();
            assignment.Drone.Router.Clear();
            return assignment;
        }

        private bool CancelPending(PilotSession session)
        {
            if (!pending.TryGetValue(session, out var previous))
            {
                return false;
            }
            pending.Remove(session);
            previous.Cancel();
            previous.Dispose();
            EndPending(session.PilotId);
            return true;
        }

        private void Settle(PilotId pilot, AuthorizationState withoutADrone)
        {
            authorizations[pilot] = TryGetDrone(pilot, out _) ? AuthorizationState.Granted : withoutADrone;
        }

        private void EndPending(PilotId pilot)
        {
            if (AuthorizationOf(pilot) == AuthorizationState.Pending)
            {
                Settle(pilot, AuthorizationState.Unauthorized);
            }
        }

        // A request still waiting for its answer outlives the grant in use.
        private void LoseGrant(PilotSession session, AuthorizationState state)
        {
            authorizations[session.PilotId] = pending.ContainsKey(session) ? AuthorizationState.Pending : state;
        }

        private FlightRequestResult Deny(PilotSession session, DroneId? preferred, FlightDenial denial, string message)
        {
            EndPending(session.PilotId);
            Raise(Denied, new FlightDenied(session.PilotId, preferred, denial, message));
            return new FlightRequestResult(denial, message);
        }

        private void Raise<T>(Action<T> listeners, T change)
        {
            if (listeners == null)
            {
                return;
            }
            foreach (var listener in listeners.GetInvocationList())
            {
                try
                {
                    ((Action<T>)listener)(change);
                }
                catch (Exception exception)
                {
                    ListenerFailed?.Invoke(exception);
                }
            }
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

        // Who flies a drone, and the registration that releases it if their grant is revoked.
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
