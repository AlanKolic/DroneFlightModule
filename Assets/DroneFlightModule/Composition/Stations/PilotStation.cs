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
using UnityEngine.InputSystem;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// One input device and whoever flies with it. It owns the device's controls, input and keys,
    /// and holds a session for every pilot in the registry, so a pilot can fly as anyone from it.
    /// It knows nothing of other stations; they meet only in the coordinator, so it reports only
    /// the drone its own session steers. Switching keeps the previous pilot on their drone with
    /// their hands off the sticks, so the next pilot can outrank and take it over, and cancels the
    /// previous pilot's pending request, so nobody is given a drone after they were switched away
    /// from. Its link can be cut, and it reports no pilot while its device is unplugged. It keeps
    /// the latest outcome for display, including a pilot here losing their drone. Disposing stops
    /// listening and destroys its controls.
    /// </summary>
    public sealed class PilotStation : IStationStatus, IDisposable
    {
        private readonly FlightCoordinator coordinator;
        private readonly InputActionAsset controls;
        private readonly InputActionInputProvider input;
        private readonly StationKeys keys;
        private readonly ObjectDestroyer destroyer = new ObjectDestroyer();
        private readonly List<PilotSession> sessions = new List<PilotSession>();
        private readonly CancellationToken lifetime;

        public PilotStation(
            string name,
            InputDevice device,
            InputActionAsset controls,
            FlightCoordinator coordinator,
            IPilotRegistry pilots,
            IAuthorizationProviderSelector providers,
            PilotId startPilot,
            CancellationToken lifetime)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A station needs a name.", nameof(name));
            }
            Name = name;
            Device = device ?? throw new ArgumentNullException(nameof(device));
            this.controls = controls ?? throw new ArgumentNullException(nameof(controls));
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            this.lifetime = lifetime;

            input = new InputActionInputProvider(controls);
            Link = new DisconnectableInputProvider(input);
            foreach (var profile in pilots.Profiles)
            {
                sessions.Add(new PilotSession(profile.Id, providers.For(profile), new LocalPilotInput(Link)));
            }
            Session = sessions.Find(session => session.PilotId == startPilot)
                ?? throw new ArgumentException($"{startPilot} is not in the pilot registry.", nameof(startPilot));
            SetSelected(Session, true);
            keys = new StationKeys(controls, this);

            coordinator.Released += OnReleased;
            coordinator.TookOver += OnTookOver;
            coordinator.Moved += OnMoved;
        }

        public string Name { get; }
        public InputDevice Device { get; private set; }
        public DisconnectableInputProvider Link { get; }
        public PilotSession Session { get; private set; }
        public string LastMessage { get; private set; } = "Request a drone to fly.";
        public string LinkStatus => Link.IsLinkCut ? "link cut" : Link.IsConnected ? "connected" : "disconnected";

        public bool TryGetDrone(out DroneId drone)
        {
            return coordinator.TryGetDrone(Session, out drone);
        }

        public string Describe()
        {
            string flying = TryGetDrone(out var drone) ? $"flying {drone}" : "no drone";
            return $"{Name}: {Session.PilotId}, {flying}, {LinkStatus}";
        }

        public bool HoldsADrone
        {
            get
            {
                foreach (var session in sessions)
                {
                    if (coordinator.TryGetDrone(session, out _))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        // Also for the same device returning: removing it took it out of the controls' device list.
        public void Attach(InputDevice device)
        {
            Device = device ?? throw new ArgumentNullException(nameof(device));
            controls.devices = new[] { device };
        }

        // Never faults: a superseded or cancelled request ends quietly, and anything else is logged and reported.
        public async Task RequestFlightAsync(DroneId? drone)
        {
            var session = Session;
            LastMessage = $"Asking {session.Provider.Id} for {(drone.HasValue ? drone.Value.ToString() : "any drone")}...";
            try
            {
                var result = await coordinator.RequestFlightAsync(session, drone, lifetime);
                LastMessage = result.Message;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                LastMessage = $"The request failed: {exception.Message}";
            }
        }

        public Task RequestDroneAsync(int number)
        {
            return RequestFlightAsync(new DroneId($"drone-{number}"));
        }

        public bool Release()
        {
            bool released = coordinator.Release(Session);
            LastMessage = released ? "Released the drone." : "Not flying a drone.";
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
            SetSelected(previous, false);
            SetSelected(session, true);
            Session = session;
            string cancelled = coordinator.CancelRequest(previous) ? $" {previous.PilotId}'s request was cancelled." : "";
            string kept = coordinator.TryGetDrone(previous, out var parked)
                ? $" {previous.PilotId} keeps {parked}, hands off."
                : "";
            LastMessage = $"Now flying as {Session.PilotId}, authorized by {Session.Provider.Id}.{cancelled}{kept}";
        }

        public bool RevokeServerGrant()
        {
            var pilot = Session.PilotId;
            bool revoked = Session.Provider is IRevokingAuthorizationProvider server && server.Revoke(pilot);
            LastMessage = revoked ? $"The server revoked {pilot}'s grant." : $"{pilot} holds no server grant.";
            return revoked;
        }

        public void ToggleLink()
        {
            Link.IsLinkCut = !Link.IsLinkCut;
            LastMessage = Link.IsLinkCut ? "Link cut." : "Link restored.";
        }

        public void Dispose()
        {
            coordinator.Released -= OnReleased;
            coordinator.TookOver -= OnTookOver;
            coordinator.Moved -= OnMoved;
            keys.Dispose();
            input.Dispose();
            destroyer.Destroy(controls);
        }

        private void SetSelected(PilotSession session, bool isSelected)
        {
            ((LocalPilotInput)session.Input).IsSelected = isSelected;
        }

        private void OnReleased(DroneReleased released)
        {
            if (!sessions.Contains(released.Session))
            {
                return;
            }
            if (released.Reason == ReleaseReason.Removed)
            {
                LastMessage = $"{released.Drone} was removed from the fleet.";
            }
            else if (released.Reason == ReleaseReason.LinkLost)
            {
                LastMessage = $"{released.Drone} landed with no link and was released.";
            }
        }

        // The pilot who took over hears it from their own request; a pilot here, flying or parked, hears here that they lost a drone.
        private void OnTookOver(DroneTakenOver takenOver)
        {
            if (sessions.Contains(takenOver.FromSession))
            {
                LastMessage = $"{takenOver.Pilot} took {takenOver.Drone} over from {takenOver.FromPilot}.";
            }
        }

        private void OnMoved(DroneMoved moved)
        {
            if (sessions.Contains(moved.FromSession))
            {
                LastMessage = $"{moved.Pilot} moved {moved.Drone} to another device.";
            }
        }
    }
}
