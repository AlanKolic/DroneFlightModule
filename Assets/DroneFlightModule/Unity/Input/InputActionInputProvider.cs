using System;
using DroneFlightModule.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace DroneFlightModule.Unity
{
    /// <summary>
    /// Local pilot driven by the Flight map of an Input System actions asset, so bindings and devices
    /// are data rather than code. Attached to one device, it reads only that device. macOS can drop a
    /// Bluetooth pad unreported, so with a silence timeout a device that sends nothing counts as lost
    /// until it next sends something. Centred sticks only hover, so they can have a longer timeout,
    /// while a deflected stick might be stale input flying the drone away.
    /// </summary>
    public sealed class InputActionInputProvider : IInputProvider, IDisposable
    {
        private readonly ArgumentGuard guard = new ArgumentGuard();
        private readonly InputActionAsset actions;
        private readonly float silenceTimeout;
        private readonly float centredSilenceTimeout;
        private readonly InputActionMap map;
        private readonly InputAction move;
        private readonly InputAction climb;
        private readonly InputAction yaw;
        private InputDevice device;
        private double silenceStart;

        public InputActionInputProvider(InputActionAsset actions, float silenceTimeout = 0f, float centredSilenceTimeout = 0f)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }
            this.actions = actions;
            this.silenceTimeout = guard.RequireNonNegative(silenceTimeout, nameof(silenceTimeout));
            this.centredSilenceTimeout = Math.Max(silenceTimeout, guard.RequireNonNegative(centredSilenceTimeout, nameof(centredSilenceTimeout)));
            map = actions.FindActionMap("Flight", throwIfNotFound: true);
            move = map.FindAction("Move", throwIfNotFound: true);
            climb = map.FindAction("Climb", throwIfNotFound: true);
            yaw = map.FindAction("Yaw", throwIfNotFound: true);
            map.Enable();
        }

        public bool IsConnected => map.enabled && !IsSilent && (move.controls.Count > 0 || climb.controls.Count > 0 || yaw.controls.Count > 0);

        private bool IsSilent
        {
            get
            {
                if (device == null || silenceTimeout <= 0f)
                {
                    return false;
                }
                float timeout = IsCentred ? centredSilenceTimeout : silenceTimeout;
                return InputState.currentTime - Math.Max(device.lastUpdateTime, silenceStart) > timeout;
            }
        }

        private bool IsCentred => move.ReadValue<Vector2>() == Vector2.zero && climb.ReadValue<float>() == 0f && yaw.ReadValue<float>() == 0f;

        // Also for the same device returning: removing it took it out of the actions' device list.
        public void Attach(InputDevice device)
        {
            this.device = device ?? throw new ArgumentNullException(nameof(device));
            actions.devices = new[] { device };
            ResetSilence();
        }

        public void ResetSilence()
        {
            silenceStart = InputState.currentTime;
        }

        public ControlInput Read()
        {
            if (!map.enabled)
            {
                return ControlInput.Zero;
            }
            var planar = move.ReadValue<Vector2>();
            return new ControlInput(new Vec3(planar.x, climb.ReadValue<float>(), planar.y), yaw.ReadValue<float>());
        }

        public void Dispose()
        {
            map.Disable();
        }
    }
}
