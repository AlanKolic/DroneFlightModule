using System;
using DroneFlightModule.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DroneFlightModule.Unity
{
    /// <summary>
    /// Local pilot driven by the Flight map of an Input System actions asset, so bindings and
    /// devices (keyboard, gamepad) are data rather than code. Connected while any bound device is present.
    /// </summary>
    public sealed class InputActionInputProvider : IInputProvider, IDisposable
    {
        private readonly InputActionMap map;
        private readonly InputAction move;
        private readonly InputAction climb;
        private readonly InputAction yaw;

        public InputActionInputProvider(InputActionAsset actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }
            map = actions.FindActionMap("Flight", throwIfNotFound: true);
            move = map.FindAction("Move", throwIfNotFound: true);
            climb = map.FindAction("Climb", throwIfNotFound: true);
            yaw = map.FindAction("Yaw", throwIfNotFound: true);
            map.Enable();
        }

        public bool IsConnected => move.controls.Count > 0 || climb.controls.Count > 0 || yaw.controls.Count > 0;

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
