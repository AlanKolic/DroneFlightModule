using DroneFlightModule.Core;
using UnityEngine;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Editable limits and gains of the velocity controller, mapped to a
    /// <see cref="VelocityControllerConfig"/>, which validates them. Defaults match
    /// <see cref="VelocityControllerConfig.Default"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "DroneFlightModule/Velocity Controller", fileName = "VelocityController")]
    public sealed class VelocityControllerAsset : ScriptableObject
    {
        [SerializeField] private float maxHorizontalSpeed = 8f;
        [SerializeField] private float maxClimbRate = 3f;
        [SerializeField] private float maxYawRate = 1.5f;
        [SerializeField] private float horizontalSpeedGain = 2f;
        [SerializeField] private float verticalSpeedGain = 4f;
        [SerializeField] private float yawRateGain = 8f;

        public VelocityControllerConfig ToConfig()
        {
            return new VelocityControllerConfig(
                maxHorizontalSpeed, maxClimbRate, maxYawRate, horizontalSpeedGain, verticalSpeedGain, yawRateGain);
        }
    }
}
