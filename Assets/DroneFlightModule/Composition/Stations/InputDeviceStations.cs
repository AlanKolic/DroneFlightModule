using System;
using System.Collections.Generic;
using System.Linq;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;
using UnityEngine.InputSystem;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Gives every pilot input device its own <see cref="PilotStation"/>: each device of a
    /// configured <see cref="InputDeviceGroup"/> present at start, in the groups' order, and each
    /// one plugged in later; a device matching no group, such as a mouse by default, gets none.
    /// A group's layout must be known to the Input System and its binding group must be one of the
    /// controls' control schemes. Unplugging a device is link loss: its controls resolve to nothing,
    /// no other device can stand in for it, and the failsafe takes over; plugging the same device
    /// back in resumes it. A new station starts as the lowest-ranked pilot no other station flies
    /// as, so the guest first and an unknown device is never the strongest pilot, or as the guest
    /// when everyone is taken. Disposing disposes every station.
    /// </summary>
    public sealed class InputDeviceStations : IDisposable
    {
        private readonly IReadOnlyList<InputDeviceGroup> deviceGroups;
        private readonly PilotStationFactory factory;
        private readonly IPilotRegistry pilots;
        private readonly List<PilotStation> stations = new List<PilotStation>();
        private readonly Dictionary<string, int> stationCounts = new Dictionary<string, int>();

        public InputDeviceStations(
            IReadOnlyList<InputDeviceGroup> deviceGroups,
            InputActionAsset controls,
            PilotStationFactory factory,
            IPilotRegistry pilots)
        {
            this.deviceGroups = deviceGroups ?? throw new ArgumentNullException(nameof(deviceGroups));
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.pilots = pilots ?? throw new ArgumentNullException(nameof(pilots));
            if (controls == null)
            {
                throw new ArgumentNullException(nameof(controls));
            }
            RequireKnown(controls);

            // Group by group, so the first group's devices (the keyboard by default) come first and start as the guest.
            foreach (var deviceGroup in deviceGroups)
            {
                foreach (var device in InputSystem.devices)
                {
                    if (GroupOf(device) == deviceGroup)
                    {
                        Add(device);
                    }
                }
            }
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        public IReadOnlyList<PilotStation> Stations => stations;
        public event Action<PilotStation> StationAdded;

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
            if (change != InputDeviceChange.Added)
            {
                return;
            }
            var returning = stations.Find(station => station.Device == device);
            if (returning != null)
            {
                returning.RestoreDevice();
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
            string group = deviceGroup.BindingGroup;
            stationCounts.TryGetValue(group, out int count);
            stationCounts[group] = count + 1;

            var station = factory.Create($"{group} {count + 1}", device, group, StartPilot());
            stations.Add(station);
            StationAdded?.Invoke(station);
        }

        // The first group whose layout the device's layout is, or is based on.
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

        private PilotId StartPilot()
        {
            var taken = new HashSet<PilotId>(stations.Select(station => station.Session.PilotId));
            // Profiles are listed highest priority first, with the guest last.
            for (int i = pilots.Profiles.Count - 1; i >= 0; i--)
            {
                if (!taken.Contains(pilots.Profiles[i].Id))
                {
                    return pilots.Profiles[i].Id;
                }
            }
            return PilotProfile.Guest.Id;
        }
    }
}
