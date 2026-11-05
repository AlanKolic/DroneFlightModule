using System;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Which input devices get a pilot station, and which binding group of the controls they fly
    /// with: every device whose Input System layout is, or is based on, the given layout (such as
    /// "Keyboard" or "Gamepad"). Whether both exist is checked against the Input System and the
    /// controls when the stations are made.
    /// </summary>
    public sealed class InputDeviceGroup
    {
        public InputDeviceGroup(string layout, string bindingGroup)
        {
            if (string.IsNullOrWhiteSpace(layout))
            {
                throw new ArgumentException("A device group needs a device layout.", nameof(layout));
            }
            if (string.IsNullOrWhiteSpace(bindingGroup))
            {
                throw new ArgumentException("A device group needs a binding group.", nameof(bindingGroup));
            }
            Layout = layout;
            BindingGroup = bindingGroup;
        }

        public string Layout { get; }
        public string BindingGroup { get; }

        public override string ToString() => $"{Layout} ({BindingGroup})";
    }
}
