namespace DroneFlightModule.Core
{
    /// <summary>
    /// Limits and gains of the velocity controller: full stick asks for the max speed, and each
    /// gain (1/s) sets how quickly the drone reaches it. Speeds in m/s, yaw rate in rad/s.
    /// </summary>
    public sealed class VelocityControllerConfig
    {
        public static readonly VelocityControllerConfig Default = new VelocityControllerConfig(
            maxHorizontalSpeed: 8f,
            maxClimbRate: 3f,
            maxYawRate: 1.5f,
            horizontalSpeedGain: 2f,
            verticalSpeedGain: 4f,
            yawRateGain: 8f);

        private readonly ArgumentGuard guard = new ArgumentGuard();

        public float MaxHorizontalSpeed { get; }
        public float MaxClimbRate { get; }
        public float MaxYawRate { get; }
        public float HorizontalSpeedGain { get; }
        public float VerticalSpeedGain { get; }
        public float YawRateGain { get; }

        public VelocityControllerConfig(
            float maxHorizontalSpeed,
            float maxClimbRate,
            float maxYawRate,
            float horizontalSpeedGain,
            float verticalSpeedGain,
            float yawRateGain)
        {
            MaxHorizontalSpeed = guard.RequirePositive(maxHorizontalSpeed, nameof(maxHorizontalSpeed));
            MaxClimbRate = guard.RequirePositive(maxClimbRate, nameof(maxClimbRate));
            MaxYawRate = guard.RequirePositive(maxYawRate, nameof(maxYawRate));
            HorizontalSpeedGain = guard.RequirePositive(horizontalSpeedGain, nameof(horizontalSpeedGain));
            VerticalSpeedGain = guard.RequirePositive(verticalSpeedGain, nameof(verticalSpeedGain));
            YawRateGain = guard.RequirePositive(yawRateGain, nameof(yawRateGain));
        }
    }
}
