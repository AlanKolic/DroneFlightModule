using System;
using DroneFlightModule.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DroneFlightModule.Unity
{
    /// <summary>
    /// On-screen debug readout for one drone, plus the controls. Only reads from the loop and the
    /// router; the text is rebuilt at most once per rendered frame, not on every OnGUI pass. The
    /// controls are described from the actions asset's bindings, so they never drift from it.
    /// </summary>
    public sealed class FlightDebugHud : MonoBehaviour
    {
        [SerializeField] private Rect area = new Rect(12f, 12f, 320f, 210f);

        private readonly FlightReadout readout = new FlightReadout();
        private FlightLoop loop;
        private InputRouter router;
        private FlightFrame lastFrame;
        private bool hasFrame;
        private bool isDirty;
        private string controlsText = string.Empty;
        private string text = string.Empty;

        public void Bind(FlightLoop loop, InputRouter router, InputActionAsset controls)
        {
            if (loop == null)
            {
                throw new ArgumentNullException(nameof(loop));
            }
            if (router == null)
            {
                throw new ArgumentNullException(nameof(router));
            }
            if (controls == null)
            {
                throw new ArgumentNullException(nameof(controls));
            }
            Unbind();
            this.loop = loop;
            this.router = router;
            controlsText = string.Join("\n",
                Describe(controls, "Flight/Move", "move"),
                Describe(controls, "Flight/Climb", "climb, descend"),
                Describe(controls, "Flight/Yaw", "yaw"),
                Describe(controls, "Debug/ToggleLink", "cut or restore the link"));
            loop.Ticked += OnTicked;
        }

        private void Update()
        {
            if (!isDirty)
            {
                return;
            }
            isDirty = false;
            text = readout.Format(lastFrame, router.ActivePilotId, router.IsConnected) + "\n\n" + controlsText;
        }

        private void OnGUI()
        {
            if (!hasFrame)
            {
                return;
            }
            GUI.Box(area, GUIContent.none);
            GUI.Label(new Rect(area.x + 8f, area.y + 6f, area.width - 16f, area.height - 12f), text);
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Unbind()
        {
            if (loop == null)
            {
                return;
            }
            loop.Ticked -= OnTicked;
            loop = null;
            router = null;
            hasFrame = false;
        }

        private string Describe(InputActionAsset controls, string action, string meaning)
        {
            return $"{controls.FindAction(action, throwIfNotFound: true).GetBindingDisplayString()}: {meaning}";
        }

        private void OnTicked(FlightFrame frame)
        {
            lastFrame = frame;
            hasFrame = true;
            isDirty = true;
        }
    }
}
