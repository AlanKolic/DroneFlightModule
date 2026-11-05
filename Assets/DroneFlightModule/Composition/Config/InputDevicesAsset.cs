using System.Collections.Generic;
using UnityEngine;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// The inputs that get a pilot station, in the order stations are made at start, each with the
    /// name its stations go by, its device layout, the binding group of the controls it flies with
    /// and how long it may stay silent. The defaults are the keyboard, the on-screen panel's virtual
    /// gamepad, then real gamepads, which get a timeout because a Bluetooth pad can drop without its
    /// removal being reported. The first match wins, so a layout goes before any it is based on.
    /// </summary>
    [CreateAssetMenu(menuName = "DroneFlightModule/Input Devices", fileName = "InputDevices")]
    public sealed class InputDevicesAsset : ScriptableObject
    {
        [SerializeField] private InputDeviceGroup[] devices =
        {
            new InputDeviceGroup("Keyboard", "Keyboard", "Keyboard"),
            new InputDeviceGroup("Screen panel", "ScreenPanelGamepad", "Gamepad"),
            new InputDeviceGroup("Gamepad", "Gamepad", "Gamepad", silenceTimeout: 15f),
        };

        public IReadOnlyList<InputDeviceGroup> Groups => devices;
    }
}
