using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// Minimal engine-agnostic 3D vector. Y is up, Z is forward, X is right.
    /// </summary>
    public readonly struct Vec3 : IEquatable<Vec3>
    {
        public static readonly Vec3 Zero = new Vec3(0f, 0f, 0f);
        public static readonly Vec3 Up = new Vec3(0f, 1f, 0f);
        public static readonly Vec3 Forward = new Vec3(0f, 0f, 1f);
        public static readonly Vec3 Right = new Vec3(1f, 0f, 0f);

        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float SquaredMagnitude => X * X + Y * Y + Z * Z;
        public float Magnitude => MathF.Sqrt(SquaredMagnitude);

        public static Vec3 operator +(Vec3 left, Vec3 right) => new Vec3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        public static Vec3 operator -(Vec3 left, Vec3 right) => new Vec3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        public static Vec3 operator -(Vec3 vector) => new Vec3(-vector.X, -vector.Y, -vector.Z);
        public static Vec3 operator *(Vec3 vector, float scalar) => new Vec3(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);
        public static Vec3 operator *(float scalar, Vec3 vector) => vector * scalar;
        public static Vec3 operator /(Vec3 vector, float scalar) => new Vec3(vector.X / scalar, vector.Y / scalar, vector.Z / scalar);

        public Vec3 RotateYaw(float yawRadians)
        {
            // Left-handed like Unity: positive yaw turns Forward towards Right.
            float cosine = MathF.Cos(yawRadians);
            float sine = MathF.Sin(yawRadians);
            return new Vec3(X * cosine + Z * sine, Y, -X * sine + Z * cosine);
        }

        public Vec3 WithY(float y) => new Vec3(X, y, Z);

        public bool Equals(Vec3 other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        public override bool Equals(object value) => value is Vec3 other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
        public static bool operator ==(Vec3 left, Vec3 right) => left.Equals(right);
        public static bool operator !=(Vec3 left, Vec3 right) => !left.Equals(right);

        public override string ToString() => $"({X:0.###}, {Y:0.###}, {Z:0.###})";
    }
}
