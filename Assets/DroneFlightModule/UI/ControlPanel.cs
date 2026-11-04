using System;
using DroneFlightModule.Coordination;
using DroneFlightModule.Core;
using UnityEngine;

namespace DroneFlightModule.UI
{
    /// <summary>
    /// The control panel, docked bottom right: a pilot dropdown, a drone dropdown and buttons for
    /// what the keys do, so nobody has to remember them. Picking a pilot switches to them, leaving
    /// the previous pilot on their drone, hands off, so a higher-priority pilot can take it over;
    /// both dropdowns say who flies what, and the status shows the pilot's takeovers and
    /// preemptions. The drone dropdown chooses what Request and Remove act on. It
    /// drives the fleet only through <see cref="IFleetConsole"/>. IMGUI has no dropdown, so each
    /// opens as a list in place.
    /// </summary>
    public sealed class ControlPanel : MonoBehaviour
    {
        private const float RowHeight = 24f;

        [SerializeField] private float width = 340f;
        [SerializeField] private float margin = 12f;

        private IFleetConsole console;
        private ControlPanelChoices choices;
        private bool isPilotListOpen;
        private bool isDroneListOpen;

        public void Bind(IFleetConsole console, ControlPanelChoices choices)
        {
            this.console = console ?? throw new ArgumentNullException(nameof(console));
            this.choices = choices ?? throw new ArgumentNullException(nameof(choices));
        }

        private void OnGUI()
        {
            if (console == null)
            {
                return;
            }
            int rows = 7 + (isPilotListOpen ? choices.Pilots.Count : 0) + (isDroneListOpen ? choices.Drones.Count + 1 : 0);
            float height = rows * RowHeight + 16f;
            GUILayout.BeginArea(new Rect(Screen.width - width - margin, Screen.height - height - margin, width, height), GUI.skin.box);

            string flying = console.TryGetDrone(out var drone) ? $"flying {drone}" : "no drone";
            GUILayout.Label($"You: {console.Pilot}, {flying}, {choices.Record(console.Pilot)}\n{console.LastMessage}", GUILayout.Height(2 * RowHeight));
            PilotDropdown();
            DroneDropdown();
            Buttons();

            GUILayout.EndArea();
        }

        private void PilotDropdown()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Pilot", GUILayout.Width(50f));
            if (GUILayout.Button(choices.Label(console.Pilot)))
            {
                isPilotListOpen = !isPilotListOpen;
                isDroneListOpen = false;
            }
            GUILayout.EndHorizontal();
            if (!isPilotListOpen)
            {
                return;
            }
            foreach (var profile in choices.Pilots)
            {
                if (GUILayout.Button(choices.Label(profile.Id)))
                {
                    console.SelectPilot(profile.Id);
                    isPilotListOpen = false;
                }
            }
        }

        private void DroneDropdown()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Drone", GUILayout.Width(50f));
            if (GUILayout.Button(choices.Label(choices.SelectedDrone)))
            {
                isDroneListOpen = !isDroneListOpen;
                isPilotListOpen = false;
            }
            GUILayout.EndHorizontal();
            if (!isDroneListOpen)
            {
                return;
            }
            DroneOption(null);
            foreach (var drone in choices.Drones)
            {
                DroneOption(drone.Id);
            }
        }

        private void DroneOption(DroneId? drone)
        {
            if (GUILayout.Button(choices.Label(drone)))
            {
                choices.SelectDrone(drone);
                isDroneListOpen = false;
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
            if (GUILayout.Button(console.IsLinkCut ? "Restore link" : "Cut link"))
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
