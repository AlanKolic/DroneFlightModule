using System;
using DroneFlightModule.Coordination;
using DroneFlightModule.Core;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// The fleet scene's console for the local pilot: forwards to the local pilot's controls, the
    /// link they fly through and the drone instantiator, so a control panel can do everything the
    /// keys do. A request runs on and reports through the local pilot's controls.
    /// </summary>
    public sealed class FleetConsole : IFleetConsole
    {
        private readonly LocalPilotControls localPilot;
        private readonly DisconnectableInputProvider link;
        private readonly DroneInstantiator drones;

        public FleetConsole(LocalPilotControls localPilot, DisconnectableInputProvider link, DroneInstantiator drones)
        {
            this.localPilot = localPilot ?? throw new ArgumentNullException(nameof(localPilot));
            this.link = link ?? throw new ArgumentNullException(nameof(link));
            this.drones = drones ?? throw new ArgumentNullException(nameof(drones));
        }

        public PilotId Pilot => localPilot.Session.PilotId;
        public string LastMessage => localPilot.LastMessage;
        public bool IsLinkCut => link.IsLinkCut;

        public bool TryGetDrone(out DroneId drone)
        {
            return localPilot.TryGetDrone(out drone);
        }

        public void SelectPilot(PilotId pilot)
        {
            localPilot.SelectPilot(pilot);
        }

        public void RequestFlight(DroneId? drone)
        {
            // The request never faults, so nothing is lost by not awaiting it.
            _ = localPilot.RequestFlightAsync(drone);
        }

        public void Release()
        {
            localPilot.Release();
        }

        public void RevokeGrant()
        {
            localPilot.RevokeServerGrant();
        }

        public void ToggleLink()
        {
            link.IsLinkCut = !link.IsLinkCut;
        }

        public void AddDrone()
        {
            drones.AddDrone();
        }

        public void RemoveDrone(DroneId drone)
        {
            drones.RemoveDrone(drone);
        }
    }
}
