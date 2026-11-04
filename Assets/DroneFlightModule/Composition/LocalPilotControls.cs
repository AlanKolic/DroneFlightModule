using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DroneFlightModule.Authorization;
using DroneFlightModule.Coordination;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// The pilot at this machine's keyboard or gamepad, driven by the Fleet action map. They fly
    /// as the guest through local guest mode, or switch to the registered pilot, who signs in
    /// through the simulated server. Their keys request any drone or drone 1 to 3, release it,
    /// switch identity and make the server revoke the registered pilot's grant. Switching ends the
    /// current session, so its drone is released. The latest outcome is kept for the HUD, and
    /// StatusChanged fires whenever it or the fleet changes.
    /// </summary>
    public sealed class LocalPilotControls : IDisposable
    {
        private readonly FlightCoordinator coordinator;
        private readonly DroneRegistry drones;
        private readonly SimulatedServerAuthorizationProvider server;
        private readonly PilotSession guest;
        private readonly PilotSession registered;
        private readonly CancellationToken lifetime;
        private readonly InputActionMap map;
        private readonly List<(InputAction Action, Action<InputAction.CallbackContext> Handler)> keys =
            new List<(InputAction Action, Action<InputAction.CallbackContext> Handler)>();
        private bool isDisposed;

        public LocalPilotControls(
            InputActionAsset actions,
            FlightCoordinator coordinator,
            DroneRegistry drones,
            LocalGuestAuthorizationProvider guestProvider,
            SimulatedServerAuthorizationProvider server,
            DisconnectableInputProvider input,
            PilotId registeredPilot,
            CancellationToken lifetime)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            this.drones = drones ?? throw new ArgumentNullException(nameof(drones));
            this.server = server ?? throw new ArgumentNullException(nameof(server));
            this.lifetime = lifetime;
            guest = new PilotSession(PilotProfile.Guest.Id, PilotOrigin.Local, guestProvider, input);
            registered = new PilotSession(registeredPilot, PilotOrigin.Local, server, input);
            Session = guest;

            map = actions.FindActionMap("Fleet", throwIfNotFound: true);
            OnRequestKey("RequestAny", () => RequestFlightAsync(null));
            OnRequestKey("RequestDrone1", () => RequestDroneAsync(1));
            OnRequestKey("RequestDrone2", () => RequestDroneAsync(2));
            OnRequestKey("RequestDrone3", () => RequestDroneAsync(3));
            OnKey("Release", () => Release());
            OnKey("SwitchPilot", SwitchPilot);
            OnKey("RevokeGrant", () => RevokeRegisteredGrant());
            map.Enable();

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
            coordinator.EndSession(Session);
            Session = Session == guest ? registered : guest;
            Report($"Now flying as {Session.PilotId}, authorized by {Session.Provider.Id}.");
        }

        public bool RevokeRegisteredGrant()
        {
            bool revoked = server.Revoke(registered.PilotId);
            Report(revoked
                ? $"The server revoked {registered.PilotId}'s grant."
                : $"{registered.PilotId} holds no server grant.");
            return revoked;
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }
            isDisposed = true;
            foreach (var key in keys)
            {
                key.Action.performed -= key.Handler;
            }
            map.Disable();
            coordinator.Assigned -= OnAssigned;
            coordinator.Released -= OnReleased;
            coordinator.TookOver -= OnTookOver;
        }

        private void OnKey(string action, Action press)
        {
            Action<InputAction.CallbackContext> handler = _ => press();
            var inputAction = map.FindAction(action, throwIfNotFound: true);
            inputAction.performed += handler;
            keys.Add((inputAction, handler));
        }

        private void OnRequestKey(string action, Func<Task> request)
        {
            // The request never faults, so nothing is lost by not awaiting it.
            OnKey(action, () => { _ = request(); });
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

        private void OnTookOver(DroneTakenOver takenOver)
        {
            StatusChanged?.Invoke();
        }
    }
}
