using System;
using UnityEngine;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Which input devices get a pilot station, and which binding group of the controls they fly
    /// with: every device whose Input System layout is, or is based on, the given layout (such as
    /// "Keyboard" or "Gamepad"). <see cref="InputDeviceStations"/> checks that both exist. A device
    /// that sends nothing for the silence timeout (seconds) counts as disconnected until it next
    /// sends something; zero never expires, for devices that always report their removal.
    /// </summary>
    [Serializable]
    public sealed class InputDeviceGroup
    {
        [SerializeField] private string layout;
        [SerializeField] private string bindingGroup;
        [SerializeField] private float silenceTimeout;

        public InputDeviceGroup(string layout, string bindingGroup, float silenceTimeout = 0f)
        {
            this.layout = layout;
            this.bindingGroup = bindingGroup;
            this.silenceTimeout = silenceTimeout;
        }

        public string Layout => layout;
        public string BindingGroup => bindingGroup;
        public float SilenceTimeout => silenceTimeout;

        public override string ToString() => $"{Layout} ({BindingGroup})";
    }
}
