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

        private readonly ArgumentGuard guard = new ArgumentGuard();

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
            Mass = guard.RequirePositive(mass, nameof(mass));
            YawInertia = guard.RequirePositive(yawInertia, nameof(yawInertia));
            LinearDrag = guard.RequireNonNegative(linearDrag, nameof(linearDrag));
            AngularDrag = guard.RequireNonNegative(angularDrag, nameof(angularDrag));
            MaxThrust = guard.RequireNonNegative(maxThrust, nameof(maxThrust));
            MaxLateralForce = guard.RequireNonNegative(maxLateralForce, nameof(maxLateralForce));
            MaxYawTorque = guard.RequireNonNegative(maxYawTorque, nameof(maxYawTorque));
        }
    }
}
