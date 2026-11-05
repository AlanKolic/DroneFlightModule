using System;
using System.Collections.Generic;
using DroneFlightModule.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DroneFlightModule.Unity
{
    /// <summary>
    /// On-screen debug readout for the fleet: a box per drone along the top, wrapping into more rows
    /// as drones join and closing up as they leave, below them a box per pilot station, and then
    /// the controls. Only reads from the loops, routers and stations; each drone's text is rebuilt
    /// at most once per rendered frame, the stations' four times a second, and OnGUI only draws. The controls are described from the actions asset's bindings, so they
    /// never drift from it.
    /// </summary>
    public sealed class FlightDebugHud : MonoBehaviour
    {
        [SerializeField] private Vector2 origin = new Vector2(12f, 12f);
        [SerializeField] private Vector2 droneBoxSize = new Vector2(240f, 116f);
        [SerializeField] private Vector2 stationBoxSize = new Vector2(400f, 26f);
        [SerializeField] private Vector2 controlsBoxSize = new Vector2(400f, 270f);
        [SerializeField] private float spacing = 8f;

        private readonly FlightReadout readout = new FlightReadout();
        private const float StationRefreshInterval = 0.25f;

        private readonly List<DroneReadout> drones = new List<DroneReadout>();
        private readonly List<IStationStatus> stations = new List<IStationStatus>();
        private readonly List<string> stationTexts = new List<string>();
        private string controlsText = string.Empty;
        private float nextStationRefresh;

        public void Bind(InputActionAsset controls)
        {
            if (controls == null)
            {
                throw new ArgumentNullException(nameof(controls));
            }
            controlsText = string.Join("\n",
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

        public void AddDrone(FlightLoop loop, InputRouter router)
        {
            var drone = new DroneReadout(loop ?? throw new ArgumentNullException(nameof(loop)),
                router ?? throw new ArgumentNullException(nameof(router)));
            loop.Ticked += drone.OnTicked;
            drones.Add(drone);
        }

        public void AddStation(IStationStatus station)
        {
            stations.Add(station ?? throw new ArgumentNullException(nameof(station)));
            stationTexts.Add(station.Describe());
        }

        public void RemoveStation(IStationStatus station)
        {
            int index = stations.IndexOf(station);
            if (index < 0)
            {
                return;
            }
            stations.RemoveAt(index);
            stationTexts.RemoveAt(index);
        }

        public void RemoveDrone(FlightLoop loop)
        {
            int index = drones.FindIndex(drone => drone.Loop == loop);
            if (index < 0)
            {
                return;
            }
            drones[index].Loop.Ticked -= drones[index].OnTicked;
            drones.RemoveAt(index);
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextStationRefresh)
            {
                nextStationRefresh = Time.unscaledTime + StationRefreshInterval;
                for (int i = 0; i < stations.Count; i++)
                {
                    stationTexts[i] = stations[i].Describe();
                }
            }
            foreach (var drone in drones)
            {
                if (!drone.IsDirty)
                {
                    continue;
                }
                drone.IsDirty = false;
                var frame = drone.LastFrame;
                drone.Text = $"{frame.DroneId}\n{readout.Format(frame, drone.Router.ActivePilotId, drone.Router.IsConnected)}";
            }
        }

        private void OnGUI()
        {
            int columns = Mathf.Max(1, Mathf.FloorToInt((Screen.width - origin.x + spacing) / (droneBoxSize.x + spacing)));
            for (int i = 0; i < drones.Count; i++)
            {
                if (!drones[i].HasFrame)
                {
                    continue;
                }
                float x = origin.x + i % columns * (droneBoxSize.x + spacing);
                float y = origin.y + i / columns * (droneBoxSize.y + spacing);
                DrawBox(new Rect(x, y, droneBoxSize.x, droneBoxSize.y), drones[i].Text);
            }
            int rows = (drones.Count + columns - 1) / columns;
            float top = origin.y + rows * (droneBoxSize.y + spacing);
            foreach (var text in stationTexts)
            {
                DrawBox(new Rect(origin.x, top, stationBoxSize.x, stationBoxSize.y), text);
                top += stationBoxSize.y + spacing;
            }
            var controlsBox = new Rect(origin.x, top, controlsBoxSize.x, controlsBoxSize.y);
            DrawBox(controlsBox, controlsText);
        }

        private void OnDestroy()
        {
            foreach (var drone in drones)
            {
                drone.Loop.Ticked -= drone.OnTicked;
            }
            drones.Clear();
        }

        private void DrawBox(Rect box, string text)
        {
            GUI.Box(box, GUIContent.none);
            GUI.Label(new Rect(box.x + 8f, box.y + 6f, box.width - 16f, box.height - 12f), text);
        }

        private string Describe(InputActionAsset controls, string action, string meaning)
        {
            return $"{Display(controls, action)}: {meaning}";
        }

        private string Display(InputActionAsset controls, string action)
        {
            return controls.FindAction(action, throwIfNotFound: true).GetBindingDisplayString();
        }

        private sealed class DroneReadout
        {
            public DroneReadout(FlightLoop loop, InputRouter router)
            {
                Loop = loop;
                Router = router;
            }

            public FlightLoop Loop { get; }
            public InputRouter Router { get; }
            public FlightFrame LastFrame { get; private set; }
            public bool HasFrame { get; private set; }
            public bool IsDirty { get; set; }
            public string Text { get; set; } = string.Empty;

            public void OnTicked(FlightFrame frame)
            {
                LastFrame = frame;
                HasFrame = true;
                IsDirty = true;
            }
        }
    }
}
