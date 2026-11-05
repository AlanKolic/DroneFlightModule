using System;
using System.Collections.Generic;
using DroneFlightModule.Composition;
using DroneFlightModule.Core;
using UnityEngine;

namespace DroneFlightModule.UI
{
    /// <summary>
    /// The control panel, docked bottom right, and the one place the local pilot's status shows:
    /// who they fly as, their drone, their takeovers and preemptions and the latest outcome. Below
    /// it a pilot dropdown, a drone dropdown and buttons for what the keys do. Picking a pilot
    /// switches to them, leaving the previous pilot on their drone, hands off, so a higher-priority
    /// pilot can take it over; both dropdowns say who flies what. The drone dropdown chooses what
    /// Request and Remove act on. It drives the fleet only through <see cref="IFleetConsole"/>.
    /// Text is built once per frame in Update and OnGUI only draws. IMGUI has no dropdown, so each
    /// opens as a list in place.
    /// </summary>
    public sealed class ControlPanel : MonoBehaviour
    {
        private const float RowHeight = 24f;
        private const float Padding = 16f;

        [SerializeField] private float width = 340f;
        [SerializeField] private float margin = 12f;

        private readonly GUIContent status = new GUIContent(string.Empty);
        private readonly List<string> pilotOptions = new List<string>();
        private readonly List<(DroneId? Drone, string Label)> droneOptions = new List<(DroneId? Drone, string Label)>();
        private IFleetConsole console;
        private ControlPanelChoices choices;
        private string pilotLabel = string.Empty;
        private string droneLabel = string.Empty;
        private string linkLabel = string.Empty;
        private float statusHeight;
        private bool isPilotListOpen;
        private bool isDroneListOpen;

        public void Bind(IFleetConsole console, ControlPanelChoices choices)
        {
            this.console = console ?? throw new ArgumentNullException(nameof(console));
            this.choices = choices ?? throw new ArgumentNullException(nameof(choices));
        }

        private void Update()
        {
            if (console == null)
            {
                return;
            }
            string flying = console.TryGetDrone(out var drone) ? $"flying {drone}" : "no drone";
            string text = $"You: {console.Pilot}, {flying}, {choices.Record(console.Pilot)}\n{console.LastMessage}";
            if (text != status.text)
            {
                status.text = text;
                statusHeight = 0f;
            }
            pilotLabel = choices.Label(console.Pilot);
            droneLabel = choices.Label(choices.SelectedDrone);
            linkLabel = console.IsLinkCut ? "Restore link" : "Cut link";

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
            // Measured here because the GUI skin is only available inside OnGUI; once per new text.
            if (statusHeight <= 0f)
            {
                statusHeight = GUI.skin.label.CalcHeight(status, width - Padding);
            }
            float height = statusHeight + (5 + pilotOptions.Count + droneOptions.Count) * RowHeight + Padding;
            GUILayout.BeginArea(new Rect(Screen.width - width - margin, Screen.height - height - margin, width, height), GUI.skin.box);

            GUILayout.Label(status, GUILayout.Height(statusHeight));
            PilotDropdown();
            DroneDropdown();
            Buttons();

            GUILayout.EndArea();
        }

        private void PilotDropdown()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Pilot", GUILayout.Width(50f));
            if (GUILayout.Button(pilotLabel))
            {
                isPilotListOpen = !isPilotListOpen;
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
