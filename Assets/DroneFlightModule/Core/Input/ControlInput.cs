using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// Normalized pilot intent, in the drone's local frame. Every axis is clamped to [-1, 1].
    /// Move: X = strafe right, Y = climb, Z = forward. Yaw: positive turns right.
    /// </summary>
    public readonly struct ControlInput
    {
        public static readonly ControlInput Zero = new ControlInput(Vec3.Zero, 0f);

        public readonly Vec3 Move;
        public readonly float Yaw;

        public ControlInput(Vec3 move, float yaw)
        {
            Move = new Vec3(Clamp(move.X), Clamp(move.Y), Clamp(move.Z));
            Yaw = Clamp(yaw);
        }

        private static float Clamp(float value) => float.IsNaN(value) ? 0f : Math.Clamp(value, -1f, 1f);

        public override string ToString() => $"move {Move} yaw {Yaw:0.###}";
    }
}
