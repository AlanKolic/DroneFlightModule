using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DroneFlightModule.Core;
using UnityEngine.InputSystem;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Every key that is not a flight stick, in one place: the Fleet map's request, release, switch
    /// and revoke keys for the local pilot, and the Debug keys that cut the link and add or remove
    /// drones. The dashboard's own toggle stays with the dashboard. Disposing unbinds them all and
    /// disables what was enabled here.
    /// </summary>
    public sealed class FleetKeys : IDisposable
    {
        private readonly LocalPilotControls localPilot;
        private readonly DisconnectableInputProvider link;
        private readonly DroneInstantiator drones;
        private readonly List<(InputAction Action, Action<InputAction.CallbackContext> Handler)> bindings =
            new List<(InputAction Action, Action<InputAction.CallbackContext> Handler)>();
        private bool isDisposed;

        public FleetKeys(InputActionAsset actions, LocalPilotControls localPilot, DisconnectableInputProvider link, DroneInstantiator drones)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }
            this.localPilot = localPilot ?? throw new ArgumentNullException(nameof(localPilot));
            this.link = link ?? throw new ArgumentNullException(nameof(link));
            this.drones = drones ?? throw new ArgumentNullException(nameof(drones));

            OnRequestKey(actions, "Fleet/RequestAny", () => localPilot.RequestFlightAsync(null));
            OnRequestKey(actions, "Fleet/RequestDrone1", () => localPilot.RequestDroneAsync(1));
            OnRequestKey(actions, "Fleet/RequestDrone2", () => localPilot.RequestDroneAsync(2));
            OnRequestKey(actions, "Fleet/RequestDrone3", () => localPilot.RequestDroneAsync(3));
            OnKey(actions, "Fleet/Release", () => localPilot.Release());
            OnKey(actions, "Fleet/SwitchPilot", localPilot.SwitchPilot);
            OnKey(actions, "Fleet/RevokeGrant", () => localPilot.RevokeServerGrant());
            OnKey(actions, "Debug/ToggleLink", () => link.IsLinkCut = !link.IsLinkCut);
            OnKey(actions, "Debug/AddDrone", () => drones.AddDrone());
            OnKey(actions, "Debug/RemoveDrone", () => drones.RemoveNewestDrone());
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }
            isDisposed = true;
            foreach (var binding in bindings)
            {
                binding.Action.performed -= binding.Handler;
                binding.Action.Disable();
            }
            bindings.Clear();
        }

        private void OnKey(InputActionAsset actions, string action, Action press)
        {
            Action<InputAction.CallbackContext> handler = _ => press();
            var inputAction = actions.FindAction(action, throwIfNotFound: true);
            inputAction.performed += handler;
            inputAction.Enable();
            bindings.Add((inputAction, handler));
        }

        private void OnRequestKey(InputActionAsset actions, string action, Func<Task> request)
        {
            // The request never faults, so nothing is lost by not awaiting it.
            OnKey(actions, action, () => { _ = request(); });
        }
    }
}
