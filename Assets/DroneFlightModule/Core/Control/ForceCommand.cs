namespace DroneFlightModule.Core
{
    /// <summary>
    /// What the controller asks of the actuators: a world-space force and a yaw torque.
    /// </summary>
    public readonly struct ForceCommand
    {
        public static readonly ForceCommand None = new ForceCommand(Vec3.Zero, 0f);

        public readonly Vec3 Force;
        public readonly float YawTorque;

        public ForceCommand(Vec3 force, float yawTorque)
        {
            Force = force;
            YawTorque = yawTorque;
        }

        public override string ToString() => $"force {Force} yawTorque {YawTorque:0.###}";
    }
}
