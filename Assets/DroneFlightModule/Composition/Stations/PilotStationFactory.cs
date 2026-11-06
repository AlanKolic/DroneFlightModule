using System;
using System.Threading;
using DroneFlightModule.Coordination;
using DroneFlightModule.Core;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Builds a station for a device: its own copy of the controls, masked to the device's binding
    /// group and attached to that device, so devices never read each other's buttons.
    /// </summary>
    public sealed class PilotStationFactory
    {
        private readonly InputActionAsset template;
        private readonly FlightCoordinator coordinator;
        private readonly PilotSeating seating;
        private readonly ISimulationClock clock;
        private readonly CancellationToken lifetime;

        public PilotStationFactory(
            InputActionAsset template,
            FlightCoordinator coordinator,
            PilotSeating seating,
            ISimulationClock clock,
            CancellationToken lifetime)
        {
            this.template = template ?? throw new ArgumentNullException(nameof(template));
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            this.seating = seating ?? throw new ArgumentNullException(nameof(seating));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.lifetime = lifetime;
        }

        public PilotStation Create(string name, InputDevice device, InputDeviceGroup group)
        {
            var controls = Object.Instantiate(template);
            controls.bindingMask = InputBinding.MaskByGroup(group.BindingGroup);
            return new PilotStation(name, device, controls, group.SilenceTimeout, new PatrolAutopilot(clock), coordinator, seating, lifetime);
        }
    }
}