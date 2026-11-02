using System;
using DroneFlightModule.Core;
using UnityEngine;

namespace DroneFlightModule.Unity
{
    /// <summary>
    /// On-screen debug readout for one drone, plus the controls. Only reads from the loop and the
    /// router; the text is rebuilt at most once per rendered frame, not on every OnGUI pass.
    /// </summary>
    public sealed class FlightDebugHud : MonoBehaviour
    {
        private const string Controls =
            "WASD / right stick: move\nSpace, Ctrl / left stick: climb, descend\nQ, E / left stick: yaw\nL / select: cut or restore the link";

        [SerializeField] private Rect area = new Rect(12f, 12f, 320f, 210f);

        private readonly FlightReadout readout = new FlightReadout();
        private FlightLoop loop;
        private InputRouter router;
        private FlightFrame lastFrame;
        private bool hasFrame;
        private bool isDirty;
        private string text = string.Empty;

        public void Bind(FlightLoop loop, InputRouter router)
        {
            if (loop == null)
            {
                throw new ArgumentNullException(nameof(loop));
            }
            if (router == null)
            {
                throw new ArgumentNullException(nameof(router));
            }
            Unbind();
            this.loop = loop;
            this.router = router;
            loop.Ticked += OnTicked;
        }

        private void Update()
        {
            if (!isDirty)
            {
                return;
            }
            isDirty = false;
            text = readout.Format(lastFrame, router.ActivePilotId, router.IsConnected) + "\n\n" + Controls;
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

        private void OnTicked(FlightFrame frame)
        {
            lastFrame = frame;
            hasFrame = true;
            isDirty = true;
        }
    }
}
