using System;
using DroneFlightModule.Composition;
using DroneFlightModule.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace DroneFlightModule.UI
{
    /// <summary>
    /// On-screen flight buttons, docked bottom left. They drive a virtual gamepad, so to the rest of
    /// the fleet the panel is one more input device with its own station, defined by its entry in
    /// the input devices asset. The buttons only show while that station is the one chosen in the
    /// control panel, whose Request and Release act for it. A mouse holds one
    /// button at a time, so moves can't be combined. The device is removed with the panel.
    /// </summary>
    public sealed class ScreenFlightPanel : MonoBehaviour
    {
        private const float ButtonSize = 64f;

        [SerializeField] private float margin = 15f;

        private IFleetConsole console;
        private Vector2 move;
        private Vector2 climbAndYaw;

        public Gamepad Device { get; private set; }
        public bool IsVisible => console != null && console.IsChosen(Device);

        public void Bind(IFleetConsole console)
        {
            this.console = console ?? throw new ArgumentNullException(nameof(console));
            Device = InputSystem.AddDevice<ScreenPanelGamepad>();
        }

        public void SetSticks(Vector2 move, Vector2 climbAndYaw)
        {
            if (move == this.move && climbAndYaw == this.climbAndYaw)
            {
                return;
            }
            this.move = move;
            this.climbAndYaw = climbAndYaw;
            InputSystem.QueueStateEvent(Device, new GamepadState { rightStick = move, leftStick = climbAndYaw });
        }

        public void RemoveDevice()
        {
            if (Device != null && Device.added)
            {
                InputSystem.RemoveDevice(Device);
            }
        }

        private void OnGUI()
        {
            if (console == null)
            {
                return;
            }
            var heldMove = Vector2.zero;
            var heldClimbAndYaw = Vector2.zero;
            if (IsVisible)
            {
                float top = Screen.height - 3f * ButtonSize - margin;
                heldMove = DrawPad(new Vector2(margin, top), "Forward", "Back", "Left", "Right");
                heldClimbAndYaw = DrawPad(new Vector2(margin + 3.5f * ButtonSize, top), "Climb", "Descend", "Yaw left", "Yaw right");
            }
            // A repeat button only says reliably whether it is held during Repaint.
            if (Event.current.type == EventType.Repaint)
            {
                SetSticks(heldMove, heldClimbAndYaw);
            }
        }

        private void OnDestroy()
        {
            RemoveDevice();
        }

        private Vector2 DrawPad(Vector2 corner, string up, string down, string left, string right)
        {
            bool isUp = GUI.RepeatButton(Cell(corner, 1, 0), up);
            bool isLeft = GUI.RepeatButton(Cell(corner, 0, 1), left);
            bool isRight = GUI.RepeatButton(Cell(corner, 2, 1), right);
            bool isDown = GUI.RepeatButton(Cell(corner, 1, 2), down);
            return new Vector2((isRight ? 1f : 0f) - (isLeft ? 1f : 0f), (isUp ? 1f : 0f) - (isDown ? 1f : 0f));
        }

        private Rect Cell(Vector2 corner, int column, int row)
        {
            return new Rect(corner.x + column * ButtonSize, corner.y + row * ButtonSize, ButtonSize, ButtonSize);
        }
    }
}
