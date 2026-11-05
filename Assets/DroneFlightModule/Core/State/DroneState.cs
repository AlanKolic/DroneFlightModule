namespace DroneFlightModule.Core
{
    /// <summary>
    /// True kinematic state of the drone in world space. Yaw is in radians, wrapped to (-PI, PI].
    /// </summary>
    public readonly struct DroneState
    {
        public readonly Vec3 Position;
        public readonly Vec3 Velocity;
        public readonly float Yaw;
        public readonly float YawRate;
        public readonly bool IsGrounded;

        public DroneState(Vec3 position, float yaw = 0f)
            : this(position, Vec3.Zero, yaw, 0f, isGrounded: false)
        {
        }

        public DroneState(Vec3 position, Vec3 velocity, float yaw, float yawRate, bool isGrounded)
        {
            Position = position;
            Velocity = velocity;
            Yaw = yaw;
            YawRate = yawRate;
            IsGrounded = isGrounded;
        }

        public override string ToString() =>
            $"position {Position} velocity {Velocity} yaw {Yaw:0.###} yawRate {YawRate:0.###} grounded {IsGrounded}";
    }
}
