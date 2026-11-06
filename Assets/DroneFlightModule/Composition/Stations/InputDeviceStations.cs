using System;
using System.Collections.Generic;
using System.Linq;
using DroneFlightModule.Coordination;
using UnityEngine.InputSystem;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Gives every pilot input device its own <see cref="PilotStation"/>: each device of a
    /// configured <see cref="InputDeviceGroup"/> present at start, in the groups' order, and each
    /// one plugged in later; a device matching no group, such as a mouse by default, gets none. A
    /// group's layout must be known to the Input System and its binding group must be one of the
    /// controls' control schemes. A physical pad is known by its layout and serial, not by its
    /// device object: the Editor can keep a stale entry of a pad that disconnected unseen, and the
    /// pad then connects as a second device, which takes over the stale entry's station. Unplugging
    /// a device, or its going silent past its group's timeout, is link loss, and the failsafe takes
    /// over; unplugging also turns the station's autopilot off, so a lost device can't fly on. While
    /// a gone device's pilot still holds a drone its station waits, and the pad resumes it on
    /// returning. Once a gone device holds no drone its pilot leaves and its station is
    /// removed. A new station takes the guest if nobody has them, and otherwise starts empty.
    /// Disposing disposes every station.
    /// </summary>
    public sealed class InputDeviceStations : IDisposable
    {
        private readonly IReadOnlyList<InputDeviceGroup> deviceGroups;
        private readonly PilotStationFactory factory;
        private readonly PilotSeating seating;
        private readonly List<PilotStation> stations = new List<PilotStation>();

        public InputDeviceStations(
            IReadOnlyList<InputDeviceGroup> deviceGroups,
            InputActionAsset controls,
            PilotStationFactory factory,
            PilotSeating seating)
        {
            this.deviceGroups = deviceGroups ?? throw new ArgumentNullException(nameof(deviceGroups));
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.seating = seating ?? throw new ArgumentNullException(nameof(seating));
            if (controls == null)
            {
                throw new ArgumentNullException(nameof(controls));
            }
            RequireKnown(controls);

            foreach (var deviceGroup in deviceGroups)
            {
                foreach (var device in InputSystem.devices)
                {
                    if (GroupOf(device) == deviceGroup)
                    {
                        Connect(device);
                    }
                }
            }
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        public IReadOnlyList<PilotStation> Stations => stations;
        public event Action<PilotStation> StationAdded;
        public event Action<PilotStation> StationRemoved;

        public void RemoveUnattended()
        {
            for (int i = stations.Count - 1; i >= 0; i--)
            {
                var station = stations[i];
                if (!station.Device.added && !station.HoldsADrone)
                {
                    stations.RemoveAt(i);
                    seating.Vacate(station);
                    station.Dispose();
                    StationRemoved?.Invoke(station);
                }
            }
        }

        public void Dispose()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
            foreach (var station in stations)
            {
                station.Dispose();
            }
            stations.Clear();
        }

        // The Input System raises Added for a device that comes back too, before Reconnected.
        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Added)
            {
                Connect(device);
            }
            else if (change == InputDeviceChange.Removed)
            {
                stations.Find(station => station.Device == device)?.StopAutopilot();
            }
        }

        private void Connect(InputDevice device)
        {
            var known = stations.Find(station => IsSamePad(station.Device, device));
            if (known != null)
            {
                known.Attach(device);
                return;
            }
            Add(device);
        }

        private void Add(InputDevice device)
        {
            var deviceGroup = GroupOf(device);
            if (deviceGroup == null)
            {
                return;
            }
            var station = factory.Create(FreeName(deviceGroup.StationName), device, deviceGroup);
            stations.Add(station);
            station.SelectPilot(seating.StartPilot());
            StationAdded?.Invoke(station);
        }

        private bool IsSamePad(InputDevice first, InputDevice second)
        {
            if (first == second)
            {
                return true;
            }
            string serial = first.description.serial;
            return !string.IsNullOrEmpty(serial) && serial == second.description.serial && first.layout == second.layout;
        }

        // Just the name for the first station of a group, then numbered from 2.
        private string FreeName(string name)
        {
            string free = name;
            for (int number = 2; stations.Exists(station => station.Name == free); number++)
            {
                free = $"{name} {number}";
            }
            return free;
        }

        private InputDeviceGroup GroupOf(InputDevice device)
        {
            foreach (var deviceGroup in deviceGroups)
            {
                if (InputSystem.IsFirstLayoutBasedOnSecond(device.layout, deviceGroup.Layout))
                {
                    return deviceGroup;
                }
            }
            return null;
        }

        private void RequireKnown(InputActionAsset controls)
        {
            var layouts = new HashSet<string>(InputSystem.ListLayouts(), StringComparer.OrdinalIgnoreCase);
            foreach (var deviceGroup in deviceGroups)
            {
                if (string.IsNullOrWhiteSpace(deviceGroup.StationName))
                {
                    throw new ArgumentException($"The device group {deviceGroup} needs a station name.", nameof(deviceGroups));
                }
                if (string.IsNullOrEmpty(deviceGroup.Layout) || !layouts.Contains(deviceGroup.Layout))
                {
                    throw new ArgumentException($"The Input System has no device layout '{deviceGroup.Layout}'.", nameof(deviceGroups));
                }
                if (!controls.controlSchemes.Any(scheme => scheme.bindingGroup == deviceGroup.BindingGroup))
                {
                    throw new ArgumentException($"{controls.name} has no control scheme with the binding group '{deviceGroup.BindingGroup}'.", nameof(deviceGroups));
                }
            }
        }
    }
}
