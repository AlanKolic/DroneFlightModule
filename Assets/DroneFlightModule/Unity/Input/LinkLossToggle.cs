using System;
using DroneFlightModule.Core;
using UnityEngine.InputSystem;

namespace DroneFlightModule.Unity
{
    /// <summary>
    /// Debug control: the Debug/ToggleLink action cuts or restores the local pilot's link, so the
    /// failsafe hold and descent can be seen without a real connection drop.
    /// </summary>
    public sealed class LinkLossToggle : IDisposable
    {
        private readonly InputAction toggle;
        private readonly DisconnectableInputProvider link;

        public LinkLossToggle(InputActionAsset actions, DisconnectableInputProvider link)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }
            this.link = link ?? throw new ArgumentNullException(nameof(link));
            toggle = actions.FindAction("Debug/ToggleLink", throwIfNotFound: true);
            toggle.performed += OnToggle;
            toggle.Enable();
        }

        public void Dispose()
        {
            toggle.performed -= OnToggle;
            toggle.Disable();
        }

        private void OnToggle(InputAction.CallbackContext context)
        {
            link.IsLinkCut = !link.IsLinkCut;
        }
    }
}
