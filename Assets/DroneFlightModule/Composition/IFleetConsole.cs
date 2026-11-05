using DroneFlightModule.Core;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// What the pilot at this machine can do from a control panel: choose which registered pilot
    /// to fly as, request a drone or any drone, release it, have the server revoke their grant,
    /// cut or restore their link, and add or remove drones. It spans pilots, links and drone
    /// spawning, so it lives with the composition that implements it; a panel needs nothing else
    /// and can be deleted with no trace here.
    /// </summary>
    public interface IFleetConsole
    {
        PilotId Pilot { get; }
        string LastMessage { get; }
        bool IsLinkCut { get; }

        bool TryGetDrone(out DroneId drone);

        void SelectPilot(PilotId pilot);

        void RequestFlight(DroneId? drone);

        void Release();

        void RevokeGrant();

        void ToggleLink();

        void AddDrone();

        void RemoveDrone(DroneId drone);
    }
}
