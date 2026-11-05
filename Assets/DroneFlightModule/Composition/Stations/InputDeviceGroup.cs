using System;
using UnityEngine;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Which input devices get a pilot station, and which binding group of the controls they fly
    /// with: every device whose Input System layout is, or is based on, the given layout (such as
    /// "Keyboard" or "Gamepad"). <see cref="InputDeviceStations"/> checks that both exist.
    /// </summary>
    [Serializable]
    public sealed class InputDeviceGroup
    {
        [SerializeField] private string layout;
        [SerializeField] private string bindingGroup;

        public InputDeviceGroup(string layout, string bindingGroup)
        {
            this.layout = layout;
            this.bindingGroup = bindingGroup;
        }

        public string Layout => layout;
        public string BindingGroup => bindingGroup;

        public override string ToString() => $"{Layout} ({BindingGroup})";
    }
}
