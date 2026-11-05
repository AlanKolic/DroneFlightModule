using System;
using System.Threading;
using DroneFlightModule.Coordination;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Builds a station for a device: its own copy of the controls, restricted to that device and
    /// its binding group, so devices never read each other's buttons.
    /// </summary>
    public sealed class PilotStationFactory
    {
        private readonly InputActionAsset template;
        private readonly FlightCoordinator coordinator;
        private readonly IPilotRegistry pilots;
        private readonly IAuthorizationProviderSelector providers;
        private readonly CancellationToken lifetime;

        public PilotStationFactory(
            InputActionAsset template,
            FlightCoordinator coordinator,
            IPilotRegistry pilots,
            IAuthorizationProviderSelector providers,
            CancellationToken lifetime)
        {
            this.template = template ?? throw new ArgumentNullException(nameof(template));
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            this.pilots = pilots ?? throw new ArgumentNullException(nameof(pilots));
            this.providers = providers ?? throw new ArgumentNullException(nameof(providers));
            this.lifetime = lifetime;
        }

        public PilotStation Create(string name, InputDevice device, string bindingGroup, PilotId startPilot)
        {
            var controls = Object.Instantiate(template);
            controls.devices = new[] { device };
            controls.bindingMask = InputBinding.MaskByGroup(bindingGroup);
            return new PilotStation(name, device, controls, coordinator, pilots, providers, startPilot, lifetime);
        }
    }
}
