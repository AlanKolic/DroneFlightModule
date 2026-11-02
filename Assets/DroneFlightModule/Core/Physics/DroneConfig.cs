using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// Physical body and actuator limits of one drone frame. SI units throughout.
    /// </summary>
    public sealed class DroneConfig
    {
        public static readonly DroneConfig Default = new DroneConfig(
            mass: 1.5f,
            yawInertia: 0.05f,
            linearDrag: 1f,
            angularDrag: 0.2f,
            maxThrust: 30f,
            maxLateralForce: 10f,
            maxYawTorque: 0.5f);

        public float Mass { get; }
        public float YawInertia { get; }
        public float LinearDrag { get; }
        public float AngularDrag { get; }
        public float MaxThrust { get; }
        public float MaxLateralForce { get; }
        public float MaxYawTorque { get; }

        public DroneConfig(
            float mass,
            float yawInertia,
            float linearDrag,
            float angularDrag,
            float maxThrust,
            float maxLateralForce,
            float maxYawTorque)
        {
            Mass = RequirePositive(mass, nameof(mass));
            YawInertia = RequirePositive(yawInertia, nameof(yawInertia));
            LinearDrag = RequireNonNegative(linearDrag, nameof(linearDrag));
            AngularDrag = RequireNonNegative(angularDrag, nameof(angularDrag));
            MaxThrust = RequireNonNegative(maxThrust, nameof(maxThrust));
            MaxLateralForce = RequireNonNegative(maxLateralForce, nameof(maxLateralForce));
            MaxYawTorque = RequireNonNegative(maxYawTorque, nameof(maxYawTorque));
        }

        private float RequirePositive(float value, string name)
        {
            if (!(value > 0f) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(name, value, "Must be positive and finite.");
            }
            return value;
        }

        private float RequireNonNegative(float value, string name)
        {
            if (!(value >= 0f) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(name, value, "Must be non-negative and finite.");
            }
            return value;
        }
    }
}
