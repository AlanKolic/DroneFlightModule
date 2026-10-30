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

        /// <summary>kg</summary>
        public float Mass { get; }

        /// <summary>kg·m² about the vertical axis.</summary>
        public float YawInertia { get; }

        /// <summary>N per m/s of velocity.</summary>
        public float LinearDrag { get; }

        /// <summary>N·m per rad/s of yaw rate.</summary>
        public float AngularDrag { get; }

        /// <summary>N, straight up in the drone's frame, at full climb input.</summary>
        public float MaxThrust { get; }

        /// <summary>N, horizontal, at full strafe or forward input.</summary>
        public float MaxLateralForce { get; }

        /// <summary>N·m about the vertical axis, at full yaw input.</summary>
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

        private static float RequirePositive(float value, string name)
        {
            if (!(value > 0f) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(name, value, "Must be positive and finite.");
            return value;
        }

        private static float RequireNonNegative(float value, string name)
        {
            if (!(value >= 0f) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(name, value, "Must be non-negative and finite.");
            return value;
        }
    }
}
