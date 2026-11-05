using DroneFlightModule.Core;
using UnityEngine;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Editable values of the drone frame, mapped to a <see cref="DroneConfig"/>, which validates
    /// them. Defaults match <see cref="DroneConfig.Default"/>. SI units.
    /// </summary>
    [CreateAssetMenu(menuName = "DroneFlightModule/Drone Frame", fileName = "DroneFrame")]
    public sealed class DroneFrameAsset : ScriptableObject
    {
        [SerializeField] private float mass = 1.5f;
        [SerializeField] private float yawInertia = 0.05f;
        [SerializeField] private float linearDrag = 1f;
        [SerializeField] private float angularDrag = 0.2f;
        [SerializeField] private float maxThrust = 30f;
        [SerializeField] private float maxLateralForce = 10f;
        [SerializeField] private float maxYawTorque = 0.5f;

        public DroneConfig ToConfig()
        {
            return new DroneConfig(mass, yawInertia, linearDrag, angularDrag, maxThrust, maxLateralForce, maxYawTorque);
        }
    }
}
