using System;
using DroneFlightModule.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace DroneFlightModule.Unity
{
    /// <summary>
    /// Local pilot driven by the Flight map of an Input System actions asset, so bindings and
    /// devices (keyboard, gamepad) are data rather than code. Connected while the map is enabled and
    /// a bound device is present, so a disposed provider reports a lost pilot and the failsafe takes
    /// over. Attached to one device, it reads only that device, and with a silence timeout it also
    /// reports that device as lost once it has sent nothing for that long, because macOS can drop a
    /// Bluetooth pad without telling the Input System. An idle pad looks the same, so any input
    /// reconnects it at once, and a device gets the full timeout from the moment it is attached.
    /// </summary>
    public sealed class InputActionInputProvider : IInputProvider, IDisposable
    {
        private readonly ArgumentGuard guard = new ArgumentGuard();
        private readonly InputActionAsset actions;
        private readonly float silenceTimeout;
        private readonly InputActionMap map;
        private readonly InputAction move;
        private readonly InputAction climb;
        private readonly InputAction yaw;
        private InputDevice device;
        private double attachedAt;

        public InputActionInputProvider(InputActionAsset actions, float silenceTimeout = 0f)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }
            this.actions = actions;
            this.silenceTimeout = guard.RequireNonNegative(silenceTimeout, nameof(silenceTimeout));
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
                return InputState.currentTime - Math.Max(device.lastUpdateTime, attachedAt) > silenceTimeout;
            }
        }

        // Also for the same device returning: removing it took it out of the actions' device list.
        public void Attach(InputDevice device)
        {
            this.device = device ?? throw new ArgumentNullException(nameof(device));
            actions.devices = new[] { device };
            attachedAt = InputState.currentTime;
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
