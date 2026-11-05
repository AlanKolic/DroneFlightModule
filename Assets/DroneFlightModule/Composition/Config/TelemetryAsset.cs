using DroneFlightModule.Telemetry;
using UnityEngine;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Editable telemetry sample rate, mapped to a <see cref="TelemetryConfig"/>, which validates
    /// it. The default matches <see cref="TelemetryConfig.Default"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "DroneFlightModule/Telemetry", fileName = "Telemetry")]
    public sealed class TelemetryAsset : ScriptableObject
    {
        [SerializeField] private float sampleRate = 10f;

        public TelemetryConfig ToConfig()
        {
            return new TelemetryConfig(sampleRate);
        }
    }
}
