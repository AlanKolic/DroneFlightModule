using System;
using UnityEngine;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// One kind of input: the name its stations go by, and every device whose Input System layout
    /// is, or is based on, the given layout (such as "Keyboard" or "Gamepad"), flying with the given
    /// binding group of the controls. <see cref="InputDeviceStations"/> checks that all three are
    /// usable. A device that sends nothing for the silence timeout (seconds) counts as disconnected
    /// until it next sends something; zero never expires, for inputs that can sit idle and always
    /// report their removal. With its sticks centred it gets the centred timeout instead, if that
    /// is longer, so a pilot can hover hands-off.
    /// </summary>
    [Serializable]
    public sealed class InputDeviceGroup
    {
        [SerializeField] private string stationName;
        [SerializeField] private string layout;
        [SerializeField] private string bindingGroup;
        [SerializeField] private float silenceTimeout;
        [SerializeField] private float centredSilenceTimeout;

        public InputDeviceGroup(string stationName, string layout, string bindingGroup, float silenceTimeout = 0f, float centredSilenceTimeout = 0f)
        {
            this.stationName = stationName;
            this.layout = layout;
            this.bindingGroup = bindingGroup;
            this.silenceTimeout = silenceTimeout;
            this.centredSilenceTimeout = centredSilenceTimeout;
        }

        public string StationName => stationName;
        public string Layout => layout;
        public string BindingGroup => bindingGroup;
        public float SilenceTimeout => silenceTimeout;
        public float CentredSilenceTimeout => centredSilenceTimeout;

        public override string ToString() => $"{StationName}: {Layout} ({BindingGroup})";
    }
}
