using System;
using System.Collections.Generic;
using DroneFlightModule.Composition;
using DroneFlightModule.Core;
using UnityEngine;

namespace DroneFlightModule.UI
{
    /// <summary>
    /// The control panel, docked bottom right, and the one place a station's status shows: the
    /// station, who flies from it, their drone, their takeovers and preemptions and the latest
    /// outcome. Below it a station dropdown (one per input device), a pilot dropdown, a drone
    /// dropdown and buttons for what the keys do; everything but the drone buttons acts on the
    /// chosen station. Picking a pilot
    /// switches to them, leaving the previous pilot on their drone, hands off, so a higher-priority
    /// pilot can take it over; both dropdowns say who flies what. The drone dropdown chooses what
    /// Request and Remove act on. It drives the fleet only through <see cref="IFleetConsole"/>.
    /// Text is built a few times a second in Update, and at once after a click; OnGUI only draws.
    /// IMGUI has no dropdown, so each opens as a list in place, and the panel scrolls when it would
    /// not fit the screen. The dashboard reads its height to stay clear of it.
    /// </summary>
    public sealed class ControlPanel : MonoBehaviour
    {
        private const float RowHeight = 24f;
        private const float Padding = 16f;
        // Three dropdowns, two rows of buttons, and one row of slack for the skin's spacing.
        private const int FixedRows = 6;

        [SerializeField] private float width = 340f;
        [SerializeField] private float margin = 12f;
        [SerializeField] private float refreshInterval = 0.25f;

        private readonly GUIContent status = new GUIContent(string.Empty);
        private readonly List<string> stationOptions = new List<string>();
        private readonly List<string> pilotOptions = new List<string>();
        private readonly List<(DroneId? Drone, string Label)> droneOptions = new List<(DroneId? Drone, string Label)>();
        private IFleetConsole console;
        private ControlPanelChoices choices;
        private string stationLabel = string.Empty;
        private string pilotLabel = string.Empty;
        private string droneLabel = string.Empty;
        private string linkLabel = string.Empty;
        private float statusHeight;
        private float nextRefresh;
        private Vector2 scroll;
        private bool isStationListOpen;
        private bool isPilotListOpen;
        private bool isDroneListOpen;

        public float Height { get; private set; }

        public void Bind(IFleetConsole console, ControlPanelChoices choices)
        {
            this.console = console ?? throw new ArgumentNullException(nameof(console));
            this.choices = choices ?? throw new ArgumentNullException(nameof(choices));
        }

        private void Update()
        {
            if (console == null || Time.unscaledTime < nextRefresh)
            {
                return;
            }
            nextRefresh = Time.unscaledTime + refreshInterval;
            DroneId? flown = console.TryGetDrone(out var drone) ? drone : (DroneId?)null;
            string text = choices.Status(console.Station, console.Pilot, flown, console.LastMessage);
            if (text != status.text)
            {
                status.text = text;
                statusHeight = 0f;
            }
            stationLabel = console.Station;
            pilotLabel = choices.Label(console.Pilot);
            droneLabel = choices.Label(choices.SelectedDrone);
            linkLabel = console.IsLinkCut ? "Restore link" : "Cut link";

            stationOptions.Clear();
            if (isStationListOpen)
            {
                stationOptions.AddRange(console.Stations);
            }
            pilotOptions.Clear();
            if (isPilotListOpen)
            {
                foreach (var profile in choices.Pilots)
                {
                    pilotOptions.Add(choices.Label(profile.Id));
                }
            }
            droneOptions.Clear();
            if (isDroneListOpen)
            {
                droneOptions.Add((null, choices.Label((DroneId?)null)));
                foreach (var fleetDrone in choices.Drones)
                {
                    droneOptions.Add((fleetDrone.Id, choices.Label(fleetDrone.Id)));
                }
            }
        }

        private void OnGUI()
        {
            if (console == null)
            {
                return;
            }
            if (Event.current.type == EventType.MouseUp)
            {
                nextRefresh = 0f;
            }
            // Measured here because the GUI skin is only available inside OnGUI; once per new text.
            if (statusHeight <= 0f)
            {
                statusHeight = GUI.skin.label.CalcHeight(status, width - Padding);
            }
            float wanted = statusHeight + (FixedRows + stationOptions.Count + pilotOptions.Count + droneOptions.Count) * RowHeight + Padding;
            Height = Mathf.Min(wanted, Screen.height - 2f * margin);
            GUILayout.BeginArea(new Rect(Screen.width - width - margin, Screen.height - Height - margin, width, Height), GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);

            GUILayout.Label(status, GUILayout.Height(statusHeight));
            StationDropdown();
            PilotDropdown();
            DroneDropdown();
            Buttons();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void StationDropdown()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Station", GUILayout.Width(50f));
            if (GUILayout.Button(stationLabel))
            {
                isStationListOpen = !isStationListOpen;
                isPilotListOpen = false;
                isDroneListOpen = false;
            }
            GUILayout.EndHorizontal();
            foreach (var station in stationOptions)
            {
                if (GUILayout.Button(station))
                {
                    console.SelectStation(station);
                    isStationListOpen = false;
                }
            }
        }

        private void PilotDropdown()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Pilot", GUILayout.Width(50f));
            if (GUILayout.Button(pilotLabel))
            {
                isPilotListOpen = !isPilotListOpen;
                isStationListOpen = false;
                isDroneListOpen = false;
            }
            GUILayout.EndHorizontal();
            // Options are listed in the order Update built them, which is the registry's order.
            for (int i = 0; i < pilotOptions.Count && i < choices.Pilots.Count; i++)
            {
                if (GUILayout.Button(pilotOptions[i]))
                {
                    console.SelectPilot(choices.Pilots[i].Id);
                    isPilotListOpen = false;
                }
            }
        }

        private void DroneDropdown()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Drone", GUILayout.Width(50f));
            if (GUILayout.Button(droneLabel))
            {
                isDroneListOpen = !isDroneListOpen;
                isStationListOpen = false;
                isPilotListOpen = false;
            }
            GUILayout.EndHorizontal();
            foreach (var option in droneOptions)
            {
                if (GUILayout.Button(option.Label))
                {
                    choices.SelectDrone(option.Drone);
                    isDroneListOpen = false;
                }
            }
        }

        private void Buttons()
        {
            var selected = choices.SelectedDrone;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Request"))
            {
                console.RequestFlight(selected);
            }
            if (GUILayout.Button("Release"))
            {
                console.Release();
            }
            if (GUILayout.Button("Revoke grant"))
            {
                console.RevokeGrant();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(linkLabel))
            {
                console.ToggleLink();
            }
            if (GUILayout.Button("Add drone"))
            {
                console.AddDrone();
            }
            GUI.enabled = selected.HasValue;
            if (GUILayout.Button("Remove drone") && selected.HasValue)
            {
                console.RemoveDrone(selected.Value);
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }
    }
}
