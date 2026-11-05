using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.InputSystem;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Button actions bound to handlers. Binding enables the action; disposing unbinds every
    /// handler and disables what was enabled here.
    /// </summary>
    public sealed class ActionBindings : IDisposable
    {
        private readonly InputActionAsset actions;
        private readonly List<(InputAction Action, Action<InputAction.CallbackContext> Handler)> bindings =
            new List<(InputAction Action, Action<InputAction.CallbackContext> Handler)>();

        public ActionBindings(InputActionAsset actions)
        {
            this.actions = actions ?? throw new ArgumentNullException(nameof(actions));
        }

        public void OnPress(string action, Action press)
        {
            Action<InputAction.CallbackContext> handler = _ => press();
            var inputAction = actions.FindAction(action, throwIfNotFound: true);
            inputAction.performed += handler;
            inputAction.Enable();
            bindings.Add((inputAction, handler));
        }

        public void OnRequest(string action, Func<Task> request)
        {
            // The request never faults, so nothing is lost by not awaiting it.
            OnPress(action, () => { _ = request(); });
        }

        public void Dispose()
        {
            foreach (var binding in bindings)
            {
                binding.Action.performed -= binding.Handler;
                binding.Action.Disable();
            }
            bindings.Clear();
        }
    }
}
