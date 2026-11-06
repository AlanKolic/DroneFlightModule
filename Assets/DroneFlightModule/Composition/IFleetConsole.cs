using System.Collections.Generic;
using DroneFlightModule.Core;
using UnityEngine.InputSystem;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// What a control panel can do: choose a station, and for it pick a pilot, request or release a
    /// drone, arm it, swap to the autopilot, revoke the grant and cut the link; and add or remove drones.
    /// It spans stations, pilots and spawning, so it lives with the composition that implements it, and
    /// a panel can be deleted with no trace here. With no station, the station's actions do nothing.
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
