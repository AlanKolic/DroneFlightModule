using DroneFlightModule.Core;
using UnityEngine;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Editable timings and speeds of the failsafe, mapped to a <see cref="FailsafeConfig"/>,
    /// which validates them. Defaults come from <see cref="FailsafeConfig.Default"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "DroneFlightModule/Failsafe", fileName = "Failsafe")]
    public sealed class FailsafeAsset : ScriptableObject
    {
        [SerializeField] private float holdDuration = FailsafeConfig.Default.HoldDuration;
        [SerializeField] private float holdRecoveryRate = FailsafeConfig.Default.HoldRecoveryRate;
        [SerializeField] private float descentSinkRate = FailsafeConfig.Default.DescentSinkRate;
        [SerializeField] private float touchdownSinkRate = FailsafeConfig.Default.TouchdownSinkRate;
        [SerializeField] private float flareHeight = FailsafeConfig.Default.FlareHeight;
        [SerializeField] private float fallbackSinkRate = FailsafeConfig.Default.FallbackSinkRate;
        [SerializeField] private float speedGain = FailsafeConfig.Default.SpeedGain;

        public FailsafeConfig ToConfig()
        {
            return new FailsafeConfig(holdDuration, holdRecoveryRate, descentSinkRate, touchdownSinkRate, flareHeight, fallbackSinkRate, speedGain);
        }
    }
}
