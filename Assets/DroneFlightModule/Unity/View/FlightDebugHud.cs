using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DroneFlightModule.Unity
{
    /// <summary>
    /// On-screen debug readout at the top left: a box per pilot station, and below them the
    /// controls in a box sized to fit them, described from the actions asset's bindings so they
    /// never drift from it. Text sits vertically centred in every box. Only reads from the
    /// stations, four times a second, and OnGUI only draws. The drones are on the fleet dashboard.
    /// </summary>
    public sealed class FlightDebugHud : MonoBehaviour
    {
        private const float StationRefreshInterval = 0.25f;
        private const float TextPadding = 8f;

        [SerializeField] private Vector2 origin = new Vector2(12f, 12f);
        [SerializeField] private Vector2 stationBoxSize = new Vector2(400f, 26f);
        [SerializeField] private float spacing = 10f;

        private readonly List<IStationStatus> stations = new List<IStationStatus>();
        private readonly List<string> stationTexts = new List<string>();
        private Vector2 controlsBoxSize;
        private GUIStyle boxText;
        private float nextStationRefresh;

        public string ControlsText { get; private set; } = string.Empty;
        public IReadOnlyList<string> StationTexts => stationTexts;

        public void Bind(InputActionAsset controls)
        {
            if (controls == null)
            {
                throw new ArgumentNullException(nameof(controls));
            }
            controlsBoxSize = Vector2.zero;
            ControlsText = string.Join("\n",
                Describe(controls, "Flight/Move", "move"),
                Describe(controls, "Flight/Climb", "climb, descend"),
                Describe(controls, "Flight/Yaw", "yaw"),
                Describe(controls, "Fleet/RequestAny", "request any drone"),
                Describe(controls, "Fleet/RequestDrone1", "request drone 1"),
                Describe(controls, "Fleet/RequestDrone2", "request drone 2"),
                Describe(controls, "Fleet/RequestDrone3", "request drone 3"),
                Describe(controls, "Fleet/Release", "release the drone"),
                Describe(controls, "Fleet/SwitchPilot", "switch to the next pilot"),
                Describe(controls, "Fleet/RevokeGrant", "the server revokes your grant"),
                Describe(controls, "Debug/ToggleLink", "cut or restore the link"),
                Describe(controls, "Debug/AddDrone", "add a drone"),
                Describe(controls, "Debug/RemoveDrone", "remove the newest drone"));
        }

        public void AddStation(IStationStatus station)
        {
            stations.Add(station ?? throw new ArgumentNullException(nameof(station)));
            stationTexts.Add(station.Describe());
        }

        public void RemoveStation(IStationStatus station)
        {
            int index = stations.IndexOf(station);
            if (index >= 0)
            {
                stations.RemoveAt(index);
                stationTexts.RemoveAt(index);
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < nextStationRefresh)
            {
                return;
            }
            nextStationRefresh = Time.unscaledTime + StationRefreshInterval;
            for (int i = 0; i < stations.Count; i++)
            {
                stationTexts[i] = stations[i].Describe();
            }
        }

        private void OnGUI()
        {
            // Built and measured here because the GUI skin is only available inside OnGUI.
            if (boxText == null)
            {
                boxText = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, wordWrap = false };
            }
            if (controlsBoxSize == Vector2.zero)
            {
                controlsBoxSize = boxText.CalcSize(new GUIContent(ControlsText)) + Vector2.one * (2f * TextPadding);
            }

            float top = origin.y;
            foreach (var text in stationTexts)
            {
                DrawBox(new Rect(origin.x, top, stationBoxSize.x, stationBoxSize.y), text);
                top += stationBoxSize.y + spacing;
            }
            var controlsBox = new Rect(origin.x, top, controlsBoxSize.x, controlsBoxSize.y);
            DrawBox(controlsBox, ControlsText);
        }

        private void DrawBox(Rect box, string text)
        {
            GUI.Box(box, GUIContent.none);
            GUI.Label(new Rect(box.x + TextPadding, box.y, box.width - 2f * TextPadding, box.height), text, boxText);
        }

        private string Describe(InputActionAsset controls, string action, string meaning)
        {
            return $"{controls.FindAction(action, throwIfNotFound: true).GetBindingDisplayString()}: {meaning}";
        }
    }
}
