using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DroneFlightModule.Authorization;
using DroneFlightModule.Coordination;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;
using DroneFlightModule.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Gives every pilot input device its own <see cref="PilotStation"/>: each keyboard and
    /// gamepad present at start, and each one plugged in later. A station flies with its own copy
    /// of the controls, restricted to its device and that device's binding group, so devices never
    /// read each other's buttons. Unplugging a device is link loss: its controls resolve to
    /// nothing, the station reports no pilot and the failsafe takes over, while its sessions stay,
    /// and no other device can stand in for it; plugging the same device back in gives it back to
    /// its station and resumes them. A new station starts as the first pilot no
    /// other station flies as, the guest first, then the registry's order, or as the guest when
    /// everyone is taken. Disposing disposes every station and destroys their controls.
    /// </summary>
    public sealed class InputDeviceStations : IDisposable
    {
        // A new device type is one more row: the device and the binding group its station reads.
        private readonly (Type Device, string Group)[] deviceGroups =
        {
            (typeof(Keyboard), "Keyboard"),
            (typeof(Gamepad), "Gamepad"),
        };

        private readonly InputActionAsset template;
        private readonly FlightCoordinator coordinator;
        private readonly DroneRegistry drones;
        private readonly IPilotRegistry pilots;
        private readonly LocalGuestAuthorizationProvider guestProvider;
        private readonly IRevokingAuthorizationProvider server;
        private readonly FlightDebugHud hud;
        private readonly CancellationToken lifetime;
        private readonly List<Member> members = new List<Member>();
        private readonly List<PilotStation> stations = new List<PilotStation>();
        private bool isDisposed;

        public InputDeviceStations(
            InputActionAsset template,
            FlightCoordinator coordinator,
            DroneRegistry drones,
            IPilotRegistry pilots,
            LocalGuestAuthorizationProvider guestProvider,
            IRevokingAuthorizationProvider server,
            FlightDebugHud hud,
            CancellationToken lifetime)
        {
            this.template = template ?? throw new ArgumentNullException(nameof(template));
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            this.drones = drones ?? throw new ArgumentNullException(nameof(drones));
            this.pilots = pilots ?? throw new ArgumentNullException(nameof(pilots));
            this.guestProvider = guestProvider ?? throw new ArgumentNullException(nameof(guestProvider));
            this.server = server ?? throw new ArgumentNullException(nameof(server));
            this.hud = hud;
            this.lifetime = lifetime;

            // Device type by device type, so the keyboard comes first and starts as the guest. Each device is listed once.
            foreach (var deviceGroup in deviceGroups)
            {
                foreach (var device in InputSystem.devices)
                {
                    if (deviceGroup.Device.IsInstanceOfType(device))
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
            if (isDisposed)
            {
                return;
            }
            isDisposed = true;
            InputSystem.onDeviceChange -= OnDeviceChange;
            foreach (var member in members)
            {
                if (hud != null)
                {
                    hud.RemoveStation(member.Station);
                }
                member.Keys.Dispose();
                member.Station.Dispose();
                member.Input.Dispose();
                DestroyControls(member.Controls);
            }
            members.Clear();
            stations.Clear();
        }

        // Only a device's first arrival makes a station; one that comes back is given back to its station.
        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change != InputDeviceChange.Added && change != InputDeviceChange.Reconnected)
            {
                return;
            }
            var member = members.Find(candidate => candidate.Device == device);
            if (member == null)
            {
                Add(device);
                return;
            }
            // Removing a device also takes it out of every asset's device list, leaving this one empty, so it is restored.
            member.Controls.devices = new[] { device };
        }

        private void Add(InputDevice device)
        {
            string group = GroupOf(device);
            if (group == null)
            {
                return;
            }

            var controls = Object.Instantiate(template);
            controls.devices = new[] { device };
            controls.bindingMask = InputBinding.MaskByGroup(group);
            var input = new InputActionInputProvider(controls);
            var link = new DisconnectableInputProvider(input);
            int number = members.Count(member => member.Group == group) + 1;
            var station = new PilotStation(
                $"{group} {number}", coordinator, drones, pilots, guestProvider, server, link, StartPilot(), lifetime);
            var keys = new StationKeys(controls, station);

            members.Add(new Member(device, group, controls, input, station, keys));
            stations.Add(station);
            if (hud != null)
            {
                hud.AddStation(station);
            }
            StationAdded?.Invoke(station);
        }

        private string GroupOf(InputDevice device)
        {
            foreach (var deviceGroup in deviceGroups)
            {
                if (deviceGroup.Device.IsInstanceOfType(device))
                {
                    return deviceGroup.Group;
                }
            }
            return null;
        }

        private PilotId StartPilot()
        {
            var taken = new HashSet<PilotId>(stations.Select(station => station.Session.PilotId));
            if (!taken.Contains(PilotProfile.Guest.Id))
            {
                return PilotProfile.Guest.Id;
            }
            foreach (var profile in pilots.Profiles)
            {
                if (!taken.Contains(profile.Id))
                {
                    return profile.Id;
                }
            }
            return PilotProfile.Guest.Id;
        }

        private void DestroyControls(InputActionAsset controls)
        {
            // Edit mode can't defer destruction, so tests destroy at once.
            if (Application.isPlaying)
            {
                Object.Destroy(controls);
            }
            else
            {
                Object.DestroyImmediate(controls);
            }
        }

        private sealed class Member
        {
            public Member(InputDevice device, string group, InputActionAsset controls, InputActionInputProvider input, PilotStation station, StationKeys keys)
            {
                Device = device;
                Group = group;
                Controls = controls;
                Input = input;
                Station = station;
                Keys = keys;
            }

            public InputDevice Device { get; }
            public string Group { get; }
            public InputActionAsset Controls { get; }
            public InputActionInputProvider Input { get; }
            public PilotStation Station { get; }
            public StationKeys Keys { get; }
        }
    }
}
