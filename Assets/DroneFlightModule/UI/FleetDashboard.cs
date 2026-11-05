using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DroneFlightModule.UI
{
    /// <summary>
    /// The fleet dashboard, docked to the right of the screen: drones, pilots and recent events.
    /// Its text is rebuilt a few times a second rather than every frame, since it is for reading,
    /// not for flying. It stops above the control panel, however tall that is, and clips what does
    /// not fit. The toggle action (Debug/ToggleDashboard, Tab) shows or hides
    /// it; the dashboard enables that action while bound and disables it when destroyed.
    /// </summary>
    public sealed class FleetDashboard : MonoBehaviour
    {
        [SerializeField] private float width = 460f;
        [SerializeField] private float margin = 12f;
        [SerializeField] private float refreshInterval = 0.25f;
        [SerializeField] private int eventCount = 8;
        [SerializeField] private bool isVisible = true;

        private readonly GUIContent content = new GUIContent(string.Empty);
        private FleetDashboardReadout readout;
        private InputAction toggle;
        private ControlPanel panel;
        private float nextRefresh;
        private float height;

        public bool IsVisible => isVisible;

        public void Bind(FleetDashboardReadout readout, InputAction toggle, ControlPanel panel)
        {
            this.readout = readout ?? throw new ArgumentNullException(nameof(readout));
            this.panel = panel != null ? panel : throw new ArgumentNullException(nameof(panel));
            if (toggle == null)
            {
                throw new ArgumentNullException(nameof(toggle));
            }
            Unbind();
            this.toggle = toggle;
            toggle.performed += OnToggle;
            toggle.Enable();
            nextRefresh = 0f;
        }

        private void Update()
        {
            if (readout == null || !isVisible || Time.unscaledTime < nextRefresh)
            {
                return;
            }
            nextRefresh = Time.unscaledTime + refreshInterval;
            content.text = $"FLEET  (Tab hides)\n\n{readout.Drones()}\n\nPILOTS\n{readout.Pilots()}\n\nRECENT EVENTS\n{readout.Events(eventCount)}";
            height = 0f;
        }

        private void OnGUI()
        {
            if (!isVisible || readout == null)
            {
                return;
            }
            // Measured here because the GUI skin is only available inside OnGUI; once per new text.
            if (height <= 0f)
            {
                height = GUI.skin.label.CalcHeight(content, width - 16f) + 12f;
            }
            float available = Mathf.Max(0f, Screen.height - panel.Height - 3f * margin);
            var box = new Rect(Screen.width - width - margin, margin, width, Mathf.Min(height, available));
            GUI.Box(box, GUIContent.none);
            GUI.Label(new Rect(box.x + 8f, box.y + 6f, box.width - 16f, box.height - 12f), content);
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Unbind()
        {
            if (toggle == null)
            {
                return;
            }
            toggle.performed -= OnToggle;
            toggle.Disable();
            toggle = null;
        }

        private void OnToggle(InputAction.CallbackContext context)
        {
            isVisible = !isVisible;
            nextRefresh = 0f;
        }
    }
}
