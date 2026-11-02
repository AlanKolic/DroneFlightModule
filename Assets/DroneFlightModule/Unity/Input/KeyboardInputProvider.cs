using DroneFlightModule.Core;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace DroneFlightModule.Unity
{
    /// <summary>
    /// Local keyboard pilot: W/S forward and back, A/D strafe, Space climb, Q/E yaw.
    /// Reports disconnected while no keyboard is present.
    /// </summary>
    public sealed class KeyboardInputProvider : IInputProvider
    {
        public bool IsConnected => Keyboard.current != null;

        public ControlInput Read()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return ControlInput.Zero;
            }

            float strafe = Axis(keyboard.aKey, keyboard.dKey);
            float climb = keyboard.spaceKey.isPressed ? 1f : 0f;
            float forward = Axis(keyboard.sKey, keyboard.wKey);
            float yaw = Axis(keyboard.qKey, keyboard.eKey);
            return new ControlInput(new Vec3(strafe, climb, forward), yaw);
        }

        private float Axis(KeyControl negative, KeyControl positive)
        {
            return (positive.isPressed ? 1f : 0f) - (negative.isPressed ? 1f : 0f);
        }
    }
}
