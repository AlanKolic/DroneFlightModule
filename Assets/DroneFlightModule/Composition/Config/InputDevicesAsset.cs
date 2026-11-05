using System;
using System.Collections.Generic;
using UnityEngine;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// The input devices that get a pilot station, in the order stations are made at start, each
    /// with the binding group of the controls it flies with, mapped to
    /// <see cref="InputDeviceGroup"/>s. The defaults are the keyboard, then gamepads.
    /// </summary>
    [CreateAssetMenu(menuName = "DroneFlightModule/Input Devices", fileName = "InputDevices")]
    public sealed class InputDevicesAsset : ScriptableObject
    {
        [SerializeField] private DeviceEntry[] devices =
        {
            new DeviceEntry("Keyboard", "Keyboard"),
            new DeviceEntry("Gamepad", "Gamepad"),
        };

        public IReadOnlyList<InputDeviceGroup> ToGroups()
        {
            var groups = new InputDeviceGroup[devices.Length];
            for (int i = 0; i < devices.Length; i++)
            {
                groups[i] = new InputDeviceGroup(devices[i].Layout, devices[i].BindingGroup);
            }
            return groups;
        }

        [Serializable]
        private sealed class DeviceEntry
        {
            [SerializeField] private string layout;
            [SerializeField] private string bindingGroup;

            public DeviceEntry(string layout, string bindingGroup)
            {
                this.layout = layout;
                this.bindingGroup = bindingGroup;
            }

            public string Layout => layout;
            public string BindingGroup => bindingGroup;
        }
    }
}
