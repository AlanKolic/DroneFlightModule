using DroneFlightModule.Core;
using UnityEngine;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Editable values of the drone frame, mapped to a <see cref="DroneConfig"/>, which validates
    /// them. Defaults come from <see cref="DroneConfig.Default"/>. SI units.
    /// </summary>
    [CreateAssetMenu(menuName = "DroneFlightModule/Drone Frame", fileName = "DroneFrame")]
    public sealed class DroneFrameAsset : ScriptableObject
    {
        [SerializeField] private float mass = DroneConfig.Default.Mass;
        [SerializeField] private float yawInertia = DroneConfig.Default.YawInertia;
        [SerializeField] private float linearDrag = DroneConfig.Default.LinearDrag;
        [SerializeField] private float angularDrag = DroneConfig.Default.AngularDrag;
        [SerializeField] private float maxThrust = DroneConfig.Default.MaxThrust;
        [SerializeField] private float maxLateralForce = DroneConfig.Default.MaxLateralForce;
        [SerializeField] private float maxYawTorque = DroneConfig.Default.MaxYawTorque;

        public DroneConfig ToConfig()
        {
            return new DroneConfig(mass, yawInertia, linearDrag, angularDrag, maxThrust, maxLateralForce, maxYawTorque);
        }
    }
}
