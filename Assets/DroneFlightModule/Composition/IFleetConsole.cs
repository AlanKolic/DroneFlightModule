using System.Collections.Generic;
using DroneFlightModule.Core;
using UnityEngine.InputSystem;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// What a control panel can do at this machine: choose a pilot station (one per input device),
    /// and for it choose which registered pilot to fly as, request a drone or any drone, release
    /// it, have the server revoke their grant and cut or restore its link; and for the fleet, add
    /// or remove drones. It spans stations, pilots and drone spawning, so it lives with the
    /// composition that implements it; a panel needs nothing else and can be deleted with no trace
    /// here. With no station, the station's actions do nothing.
    /// </summary>
    public interface IFleetConsole
    {
        IReadOnlyList<string> Stations { get; }
        string Station { get; }
        PilotId Pilot { get; }
        string LastMessage { get; }
        bool IsLinkCut { get; }
        string LinkStatus { get; }

        void SelectStation(string station);

        bool IsChosen(InputDevice device);

        string SeatOf(PilotId pilot);

        bool TryGetDrone(out DroneId drone);

        void SelectPilot(PilotId pilot);

        void RequestFlight(DroneId? drone);

        void Release();

        void ToggleArm();

        void ToggleAutopilot();

        void RevokeGrant();

        void ToggleLink();

        void AddDrone();

        void RemoveDrone(DroneId drone);
    }
}
