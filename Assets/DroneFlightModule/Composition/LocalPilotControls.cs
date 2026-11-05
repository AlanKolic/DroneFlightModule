using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DroneFlightModule.Authorization;
using DroneFlightModule.Coordination;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;
using UnityEngine;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// The pilot at this machine. They can fly as any pilot in the registry: the guest through
    /// local guest mode, everyone else signing in through the server. They start as the guest.
    /// Switching keeps the previous pilot on their drone with their hands off the sticks, so the
    /// next pilot can outrank and take it over, and cancels the previous pilot's pending request,
    /// so nobody is given a drone after they were switched away from. The latest outcome,
    /// including a local pilot being preempted, is kept for display, and StatusChanged fires
    /// whenever it or the fleet changes.
    /// </summary>
    public sealed class LocalPilotControls : IDisposable
    {
        private readonly FlightCoordinator coordinator;
        private readonly DroneRegistry drones;
        private readonly IRevokingAuthorizationProvider server;
        private readonly List<PilotSession> sessions = new List<PilotSession>();
        private readonly Dictionary<PilotSession, LocalPilotInput> inputs = new Dictionary<PilotSession, LocalPilotInput>();
        private readonly CancellationToken lifetime;
        private bool isDisposed;

        public LocalPilotControls(
            FlightCoordinator coordinator,
            DroneRegistry drones,
            IPilotRegistry pilots,
            LocalGuestAuthorizationProvider guestProvider,
            IRevokingAuthorizationProvider server,
            DisconnectableInputProvider input,
            CancellationToken lifetime)
        {
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            this.drones = drones ?? throw new ArgumentNullException(nameof(drones));
            this.server = server ?? throw new ArgumentNullException(nameof(server));
            this.lifetime = lifetime;
            if (pilots == null)
            {
                throw new ArgumentNullException(nameof(pilots));
            }
            foreach (var profile in pilots.Profiles)
            {
                var provider = profile.IsGuest ? (IAuthorizationProvider)guestProvider : server;
                var pilotInput = new LocalPilotInput(input);
                var session = new PilotSession(profile.Id, PilotOrigin.Local, provider, pilotInput);
                sessions.Add(session);
                inputs.Add(session, pilotInput);
                if (profile.IsGuest)
                {
                    Session = session;
                }
            }
            if (Session == null)
            {
                throw new ArgumentException("The pilot registry has no guest to start as.", nameof(pilots));
            }
            inputs[Session].IsSelected = true;

            coordinator.Assigned += OnAssigned;
            coordinator.Released += OnReleased;
            coordinator.TookOver += OnTookOver;
        }

        public PilotSession Session { get; private set; }
        public string LastMessage { get; private set; } = "Press R for any drone.";
        public event Action StatusChanged;

        public bool TryGetDrone(out DroneId drone)
        {
            return coordinator.TryGetDrone(Session.PilotId, out drone);
        }

        // Never faults: a superseded or cancelled request ends quietly, and anything else is logged and reported.
        public async Task RequestFlightAsync(DroneId? drone)
        {
            var session = Session;
            Report($"Asking {session.Provider.Id} for {(drone.HasValue ? drone.Value.ToString() : "any drone")}...");
            try
            {
                var result = await coordinator.RequestFlightAsync(session, drone, lifetime);
                Report(result.Message);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Report($"The request failed: {exception.Message}");
            }
        }

        public Task RequestDroneAsync(int number)
        {
            if (number < 1 || number > drones.Drones.Count)
            {
                Report($"There is no drone {number}.");
                return Task.CompletedTask;
            }
            return RequestFlightAsync(drones.Drones[number - 1].Id);
        }

        public bool Release()
        {
            bool released = coordinator.Release(Session);
            Report(released ? "Released the drone." : "Not flying a drone.");
            return released;
        }

        public void SwitchPilot()
        {
            int next = (sessions.IndexOf(Session) + 1) % sessions.Count;
            SelectPilot(sessions[next].PilotId);
        }

        public void SelectPilot(PilotId pilot)
        {
            var session = sessions.Find(candidate => candidate.PilotId == pilot)
                ?? throw new ArgumentException($"{pilot} is not in the pilot registry.", nameof(pilot));
            if (session == Session)
            {
                return;
            }
            var previous = Session;
            inputs[previous].IsSelected = false;
            inputs[session].IsSelected = true;
            Session = session;
            string cancelled = coordinator.CancelRequest(previous) ? $" {previous.PilotId}'s request was cancelled." : "";
            string kept = coordinator.TryGetDrone(previous.PilotId, out var parked)
                ? $" {previous.PilotId} keeps {parked}, hands off."
                : "";
            Report($"Now flying as {Session.PilotId}, authorized by {Session.Provider.Id}.{cancelled}{kept}");
        }

        public bool RevokeServerGrant()
        {
            var pilot = Session.PilotId;
            bool revoked = server.Revoke(pilot);
            Report(revoked ? $"The server revoked {pilot}'s grant." : $"{pilot} holds no server grant.");
            return revoked;
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }
            isDisposed = true;
            coordinator.Assigned -= OnAssigned;
            coordinator.Released -= OnReleased;
            coordinator.TookOver -= OnTookOver;
        }

        private void Report(string message)
        {
            if (isDisposed)
            {
                return;
            }
            LastMessage = message;
            StatusChanged?.Invoke();
        }

        private void OnAssigned(DroneAssigned assigned)
        {
            StatusChanged?.Invoke();
        }

        private void OnReleased(DroneReleased released)
        {
            if (released.Reason == ReleaseReason.Removed && released.Pilot == Session.PilotId)
            {
                Report($"{released.Drone} was removed from the fleet.");
                return;
            }
            StatusChanged?.Invoke();
        }

        // The pilot who took over hears it from their request; a local pilot who lost a drone hears it here.
        private void OnTookOver(DroneTakenOver takenOver)
        {
            if (takenOver.Pilot != Session.PilotId && sessions.Exists(session => session.PilotId == takenOver.FromPilot))
            {
                Report($"{takenOver.Pilot} took {takenOver.Drone} over from {takenOver.FromPilot}.");
                return;
            }
            StatusChanged?.Invoke();
        }
    }
}
