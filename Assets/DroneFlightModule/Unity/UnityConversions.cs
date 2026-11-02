using DroneFlightModule.Core;
using UnityEngine;

namespace DroneFlightModule.Unity
{
    /// <summary>
    /// Maps Core math types to Unity's. Both use Y up, Z forward and X right, so axes map directly.
    /// </summary>
    public static class UnityConversions
    {
        public static Vector3 ToVector3(this Vec3 vector)
        {
            return new Vector3(vector.X, vector.Y, vector.Z);
        }

        public static Vec3 ToVec3(this Vector3 vector)
        {
            return new Vec3(vector.x, vector.y, vector.z);
        }

        public static Pose ToPose(this DroneState state)
        {
            return new Pose(
                state.Position.ToVector3(),
                Quaternion.AngleAxis(state.Yaw * Mathf.Rad2Deg, Vector3.up));
        }
    }
}
