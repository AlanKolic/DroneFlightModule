using System;
using System.Threading;
using System.Threading.Tasks;
using DroneFlightModule.Authorization;
using DroneFlightModule.Coordination;
using DroneFlightModule.Core;
using DroneFlightModule.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// One desk: an input device and at most one pilot. It owns the device's controls, input and
    /// keys. Who sits here is <see cref="PilotSeating"/>'s to decide, and while nobody does there
    /// is no session and the flying actions only say so. It knows nothing of other stations; they
    /// meet only in the coordinator. Its link can be cut, and it reports no pilot while its device
    /// is unplugged or silent. It keeps the latest outcome for display, including the pilot here
    /// losing their drone. Disposing stops listening and destroys its controls.
    /// </summary>
    public sealed class PilotStation : IStationStatus, IDisposable
    {
        private const string NobodyHere = "Nobody is at this desk.";

        private readonly FlightCoordinator coordinator;
        private readonly InputActionAsset controls;
        private readonly InputActionInputProvider input;
        private readonly StationKeys keys;
        private readonly ObjectDestroyer destroyer = new ObjectDestroyer();
        private readonly PilotSeating seating;
        private readonly CancellationToken lifetime;

        public PilotStation(
            string name,
            InputDevice device,
            InputActionAsset controls,
            float silenceTimeout,
            FlightCoordinator coordinator,
            PilotSeating seating,
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
            this.seating = seating ?? throw new ArgumentNullException(nameof(seating));
            this.lifetime = lifetime;

            input = new InputActionInputProvider(controls, silenceTimeout);
            input.Attach(device);
            Link = new DisconnectableInputProvider(input);
            keys = new StationKeys(controls, this);

            coordinator.Released += OnReleased;
            coordinator.TookOver += OnTookOver;
        }

        public string Name { get; }
        public InputDevice Device { get; private set; }
        public DisconnectableInputProvider Link { get; }
        public PilotSession Session { get; private set; }
        public string LastMessage { get; private set; } = "Pick a pilot for this desk.";
        public string LinkStatus => Link.IsLinkCut ? "link cut" : Link.IsConnected ? "connected" : "disconnected";

        public bool TryGetDrone(out DroneId drone)
        {
            drone = default;
            return Session != null && coordinator.TryGetDrone(Session.PilotId, out drone);
        }

        internal void Occupy(PilotSession session)
        {
            Session = session;
            // A pilot sitting down hasn't touched the device yet, and an idle pad would count as silent.
            if (session != null)
            {
                input.ResetSilence();
            }
        }

        internal void Report(string message)
        {
            LastMessage = message;
        }

        public string Describe()
        {
            if (Session == null)
            {
                return $"{Name}: empty, {LinkStatus}";
            }
            string flying = TryGetDrone(out var drone) ? $"flying {drone}" : "no drone";
            return $"{Name}: {Session.PilotId}, {coordinator.AuthorizationOf(Session.PilotId)}, {flying}, {LinkStatus}";
        }

        public bool HoldsADrone => TryGetDrone(out _);

        public void Attach(InputDevice device)
        {
            input.Attach(device);
            Device = device;
        }

        // Never faults: a superseded or cancelled request ends quietly, and anything else is logged and reported.
        public async Task RequestFlightAsync(DroneId? drone)
        {
            var session = Session;
            if (session == null)
            {
                LastMessage = NobodyHere;
                return;
            }
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
            if (Session == null)
            {
                LastMessage = NobodyHere;
                return false;
            }
            bool released = coordinator.Release(Session);
            LastMessage = released ? "Released the drone." : "Not flying a drone.";
            return released;
        }

        public void SwitchPilot()
        {
            seating.SeatNext(this);
        }

        // No pilot empties the desk.
        public void SelectPilot(PilotId pilot)
        {
            if (pilot.IsEmpty)
            {
                seating.Vacate(this);
            }
            else
            {
                seating.Seat(this, pilot);
            }
        }

        public bool RevokeServerGrant()
        {
            if (Session == null)
            {
                LastMessage = NobodyHere;
                return false;
            }
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
            keys.Dispose();
            input.Dispose();
            destroyer.Destroy(controls);
        }

        private void OnReleased(DroneReleased released)
        {
            if (released.Session != Session)
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

        // The pilot who took over hears it from their own request; the pilot here hears that they lost a drone.
        private void OnTookOver(DroneTakenOver takenOver)
        {
            if (takenOver.FromSession == Session)
            {
                LastMessage = $"{takenOver.Pilot} took {takenOver.Drone} over from {takenOver.FromPilot}.";
            }
        }
    }
}
