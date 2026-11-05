using DroneFlightModule.Core;
using UnityEngine;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Editable timings and speeds of the failsafe, mapped to a <see cref="FailsafeConfig"/>,
    /// which validates them. Defaults match <see cref="FailsafeConfig.Default"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "DroneFlightModule/Failsafe", fileName = "Failsafe")]
    public sealed class FailsafeAsset : ScriptableObject
    {
        [SerializeField] private float holdDuration = 10f;
        [SerializeField] private float descentSinkRate = 1f;
        [SerializeField] private float touchdownSinkRate = 0.3f;
        [SerializeField] private float flareHeight = 2f;
        [SerializeField] private float fallbackSinkRate = 0.5f;
        [SerializeField] private float speedGain = 4f;

        public FailsafeConfig ToConfig()
        {
            return new FailsafeConfig(holdDuration, descentSinkRate, touchdownSinkRate, flareHeight, fallbackSinkRate, speedGain);
        }
    }
}
