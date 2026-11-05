using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DroneFlightModule.Authorization;
using DroneFlightModule.Coordination;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;
using DroneFlightModule.Unity;
using UnityEngine;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// One input device and whoever flies with it. From it a pilot can fly as anyone in the
    /// registry: the guest through local guest mode, everyone else signing in through the server.
    /// It knows nothing of other stations; they meet only in the coordinator. Switching keeps the
    /// previous pilot on their drone with their hands off the sticks, so the next pilot can outrank
    /// and take it over, and cancels the previous pilot's pending request, so nobody is given a
    /// drone after they were switched away from. Its link can be cut, and it reports no pilot
    /// while its device is unplugged. The latest outcome, including the selected pilot being
    /// preempted, is kept for display, and StatusChanged fires whenever it or the fleet changes.
    /// </summary>
    public sealed class PilotStation : IStationStatus, IDisposable
    {
        private readonly FlightCoordinator coordinator;
        private readonly DroneRegistry drones;
        private readonly IRevokingAuthorizationProvider server;
        private readonly List<PilotSession> sessions = new List<PilotSession>();
        private readonly Dictionary<PilotSession, LocalPilotInput> inputs = new Dictionary<PilotSession, LocalPilotInput>();
        private readonly CancellationToken lifetime;
        private bool isDisposed;

        public PilotStation(
            string name,
            FlightCoordinator coordinator,
            DroneRegistry drones,
            IPilotRegistry pilots,
            LocalGuestAuthorizationProvider guestProvider,
            IRevokingAuthorizationProvider server,
            DisconnectableInputProvider link,
            PilotId startPilot,
            CancellationToken lifetime)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A station needs a name.", nameof(name));
            }
            Name = name;
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            this.drones = drones ?? throw new ArgumentNullException(nameof(drones));
            this.server = server ?? throw new ArgumentNullException(nameof(server));
            Link = link ?? throw new ArgumentNullException(nameof(link));
            this.lifetime = lifetime;
            if (pilots == null)
            {
                throw new ArgumentNullException(nameof(pilots));
            }
            foreach (var profile in pilots.Profiles)
            {
                var provider = profile.IsGuest ? (IAuthorizationProvider)guestProvider : server;
                var pilotInput = new LocalPilotInput(link);
                var session = new PilotSession(profile.Id, PilotOrigin.Local, provider, pilotInput);
                sessions.Add(session);
                inputs.Add(session, pilotInput);
                if (profile.Id == startPilot)
                {
                    Session = session;
                }
            }
            if (Session == null)
            {
                throw new ArgumentException($"{startPilot} is not in the pilot registry.", nameof(startPilot));
            }
            inputs[Session].IsSelected = true;

            coordinator.Assigned += OnAssigned;
            coordinator.Released += OnReleased;
            coordinator.TookOver += OnTookOver;
        }

        public string Name { get; }
        public DisconnectableInputProvider Link { get; }
        public PilotSession Session { get; private set; }
        public string LastMessage { get; private set; } = "Request a drone to fly.";
        public event Action<PilotStation> StatusChanged;

        public bool TryGetDrone(out DroneId drone)
        {
            return coordinator.TryGetDrone(Session.PilotId, out drone);
        }

        public string Describe()
        {
            string flying = TryGetDrone(out var drone) ? $"flying {drone}" : "no drone";
            string link = Link.IsLinkCut ? "link cut" : Link.IsConnected ? "connected" : "disconnected";
            return $"{Name}: {Session.PilotId}, {flying}, {link}";
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

        public void ToggleLink()
        {
            Link.IsLinkCut = !Link.IsLinkCut;
            Report(Link.IsLinkCut ? "Link cut." : "Link restored.");
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
            StatusChanged?.Invoke(this);
        }

        private void OnAssigned(DroneAssigned assigned)
        {
            StatusChanged?.Invoke(this);
        }

        private void OnReleased(DroneReleased released)
        {
            if (released.Reason == ReleaseReason.Removed && released.Pilot == Session.PilotId)
            {
                Report($"{released.Drone} was removed from the fleet.");
                return;
            }
            StatusChanged?.Invoke(this);
        }

        // The pilot who took over hears it from their request; the pilot this station flies as hears here that they lost a drone.
        private void OnTookOver(DroneTakenOver takenOver)
        {
            if (takenOver.FromPilot == Session.PilotId && takenOver.Pilot != Session.PilotId)
            {
                Report($"{takenOver.Pilot} took {takenOver.Drone} over from {takenOver.FromPilot}.");
                return;
            }
            StatusChanged?.Invoke(this);
        }
    }
}
