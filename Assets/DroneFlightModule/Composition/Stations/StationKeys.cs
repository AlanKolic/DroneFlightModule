using System;
using UnityEngine.InputSystem;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// A station's own buttons, on its own copy of the controls: the Fleet map's request, release,
    /// arm, autopilot, switch and revoke, and the Debug link toggle. Disposing unbinds them.
    /// </summary>
    public sealed class StationKeys : IDisposable
    {
        private readonly ActionBindings bindings;

        public StationKeys(InputActionAsset actions, PilotStation station)
        {
            if (station == null)
            {
                throw new ArgumentNullException(nameof(station));
            }
            bindings = new ActionBindings(actions);
            bindings.OnRequest("Fleet/RequestAny", () => station.RequestFlightAsync(null));
            bindings.OnRequest("Fleet/RequestDrone1", () => station.RequestDroneAsync(1));
            bindings.OnRequest("Fleet/RequestDrone2", () => station.RequestDroneAsync(2));
            bindings.OnRequest("Fleet/RequestDrone3", () => station.RequestDroneAsync(3));
            bindings.OnPress("Fleet/Release", () => station.Release());
            bindings.OnPress("Fleet/Arm", station.ToggleArm);
            bindings.OnPress("Fleet/ToggleAutopilot", station.ToggleAutopilot);
            bindings.OnPress("Fleet/SwitchPilot", station.SwitchPilot);
            bindings.OnPress("Fleet/RevokeGrant", () => station.RevokeServerGrant());
            bindings.OnPress("Debug/ToggleLink", station.ToggleLink);
        }

        public void Dispose()
        {
            bindings.Dispose();
        }
    }
}
