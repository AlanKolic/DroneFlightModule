using System;
using System.Collections.Generic;
using DroneFlightModule.Composition;
using DroneFlightModule.Core;
using UnityEngine;

namespace DroneFlightModule.UI
{
    /// <summary>
    /// The control panel, docked bottom right, and the one place a station's full status shows: the
    /// station, who flies from it, their drone, the link, their takeovers and preemptions and the
    /// latest outcome. Below it a station dropdown (one per input device), a pilot dropdown, a drone
    /// dropdown and buttons for what the keys do; everything but the drone buttons acts on the
    /// chosen station. Picking a pilot switches to them, leaving the previous pilot on their drone,
    /// hands off, so a higher-priority pilot can take it over; both dropdowns say who flies what.
    /// The drone dropdown chooses what Request and Remove act on. It drives the fleet only through
    /// <see cref="IFleetConsole"/>. Text is built a few times a second in Update, and at once after
    /// a click; OnGUI only draws. IMGUI has no dropdown, so one at a time opens as a list in place,
    /// and the panel scrolls when it would not fit the screen. The dashboard reads its height to
    /// stay clear of it.
    /// </summary>
    public sealed class ControlPanel : MonoBehaviour
    {
        private enum Dropdown
        {
            None,
            Station,
            Pilot,
            Drone
        }

        private const float RowHeight = 24f;
        private const float Padding = 16f;
        // Three dropdowns, two rows of buttons, and one row of slack for the skin's spacing.
        private const int FixedRows = 6;

        [SerializeField] private float width = 340f;
        [SerializeField] private float margin = 15f;
        [SerializeField] private float refreshInterval = 0.25f;

        private readonly GUIContent status = new GUIContent(string.Empty);
        private readonly List<(string Label, Action Pick)> options = new List<(string Label, Action Pick)>();
        private IFleetConsole console;
        private ControlPanelChoices choices;
        private string stationLabel = string.Empty;
        private string pilotLabel = string.Empty;
        private string droneLabel = string.Empty;
        private string linkLabel = string.Empty;
        private float statusHeight;
        private float nextRefresh;
        private Vector2 scroll;
        private Dropdown openList;
        private Dropdown listedOptions;

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
            string text = choices.Status(console.Station, console.Pilot, flown, console.LinkStatus, console.LastMessage);
            if (text != status.text)
            {
                status.text = text;
                statusHeight = 0f;
            }
            stationLabel = console.Station;
            pilotLabel = choices.Label(console.Pilot);
            droneLabel = choices.Label(choices.SelectedDrone);
            linkLabel = console.IsLinkCut ? "Restore link" : "Cut link";

            ListOptions();
        }

        private void ListOptions()
        {
            options.Clear();
            listedOptions = openList;
            switch (openList)
            {
                case Dropdown.Station:
                    foreach (var station in console.Stations)
                    {
                        options.Add((station, () => console.SelectStation(station)));
                    }
                    break;
                case Dropdown.Pilot:
                    foreach (var profile in choices.Pilots)
                    {
                        options.Add((choices.Label(profile.Id), () => console.SelectPilot(profile.Id)));
                    }
                    break;
                case Dropdown.Drone:
                    options.Add((choices.Label((DroneId?)null), () => choices.SelectDrone(null)));
                    foreach (var fleetDrone in choices.Drones)
                    {
                        options.Add((choices.Label(fleetDrone.Id), () => choices.SelectDrone(fleetDrone.Id)));
                    }
                    break;
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
            float wanted = statusHeight + (FixedRows + options.Count) * RowHeight + Padding;
            Height = Mathf.Min(wanted, Screen.height - 2f * margin);
            GUILayout.BeginArea(new Rect(Screen.width - width - margin, Screen.height - Height - margin, width, Height), GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);

            GUILayout.Label(status, GUILayout.Height(statusHeight));
            DrawDropdown("Station", stationLabel, Dropdown.Station);
            DrawDropdown("Pilot", pilotLabel, Dropdown.Pilot);
            DrawDropdown("Drone", droneLabel, Dropdown.Drone);
            Buttons();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawDropdown(string title, string label, Dropdown dropdown)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, GUILayout.Width(50f));
            if (GUILayout.Button(label))
            {
                openList = openList == dropdown ? Dropdown.None : dropdown;
            }
            GUILayout.EndHorizontal();
            // Drawn from what Update listed, not from openList, so a click can't change the controls mid-frame.
            if (listedOptions != dropdown)
            {
                return;
            }
            foreach (var option in options)
            {
                if (GUILayout.Button(option.Label))
                {
                    option.Pick();
                    openList = Dropdown.None;
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
