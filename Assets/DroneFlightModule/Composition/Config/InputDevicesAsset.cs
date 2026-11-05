using System.Collections.Generic;
using UnityEngine;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// The input devices that get a pilot station, in the order stations are made at start, each
    /// with the binding group of the controls it flies with. The defaults are the keyboard, then
    /// gamepads.
    /// </summary>
    [CreateAssetMenu(menuName = "DroneFlightModule/Input Devices", fileName = "InputDevices")]
    public sealed class InputDevicesAsset : ScriptableObject
    {
        [SerializeField] private InputDeviceGroup[] devices =
        {
            new InputDeviceGroup("Keyboard", "Keyboard"),
            new InputDeviceGroup("Gamepad", "Gamepad"),
        };

        public IReadOnlyList<InputDeviceGroup> Groups => devices;
    }
}
