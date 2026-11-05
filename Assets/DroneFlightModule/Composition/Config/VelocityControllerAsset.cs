using DroneFlightModule.Core;
using UnityEngine;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Editable limits and gains of the velocity controller, mapped to a
    /// <see cref="VelocityControllerConfig"/>, which validates them. Defaults come from
    /// <see cref="VelocityControllerConfig.Default"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "DroneFlightModule/Velocity Controller", fileName = "VelocityController")]
    public sealed class VelocityControllerAsset : ScriptableObject
    {
        [SerializeField] private float maxHorizontalSpeed = VelocityControllerConfig.Default.MaxHorizontalSpeed;
        [SerializeField] private float maxClimbRate = VelocityControllerConfig.Default.MaxClimbRate;
        [SerializeField] private float maxYawRate = VelocityControllerConfig.Default.MaxYawRate;
        [SerializeField] private float horizontalSpeedGain = VelocityControllerConfig.Default.HorizontalSpeedGain;
        [SerializeField] private float verticalSpeedGain = VelocityControllerConfig.Default.VerticalSpeedGain;
        [SerializeField] private float yawRateGain = VelocityControllerConfig.Default.YawRateGain;

        public VelocityControllerConfig ToConfig()
        {
            return new VelocityControllerConfig(
                maxHorizontalSpeed, maxClimbRate, maxYawRate, horizontalSpeedGain, verticalSpeedGain, yawRateGain);
        }
    }
}
