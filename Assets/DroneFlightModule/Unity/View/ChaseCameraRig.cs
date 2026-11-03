using DroneFlightModule.Core;
using UnityEngine;

namespace DroneFlightModule.Unity
{
    /// <summary>
    /// Chase camera maths: keeps an offset in the target's heading frame and looks at a point just
    /// above it. Damping is exponential, so the camera behaves the same at any frame rate.
    /// </summary>
    public sealed class ChaseCameraRig
    {
        private readonly ArgumentGuard guard = new ArgumentGuard();
        private readonly Vector3 offset;
        private readonly float lookHeight;
        private readonly float sharpness;

        public ChaseCameraRig(Vector3 offset, float lookHeight, float sharpness)
        {
            this.offset = offset;
            this.lookHeight = lookHeight;
            this.sharpness = guard.RequirePositive(sharpness, nameof(sharpness));
        }

        public Pose Snap(Pose target)
        {
            return LookingAt(target, target.position + target.rotation * offset, Quaternion.identity);
        }

        public Pose Follow(Pose camera, Pose target, float deltaTime)
        {
            float blend = 1f - Mathf.Exp(-sharpness * Mathf.Max(0f, deltaTime));
            var position = Vector3.Lerp(camera.position, target.position + target.rotation * offset, blend);
            return LookingAt(target, position, camera.rotation);
        }

        private Pose LookingAt(Pose target, Vector3 position, Quaternion fallback)
        {
            var direction = target.position + Vector3.up * lookHeight - position;
            var rotation = direction.sqrMagnitude > 1e-8f ? Quaternion.LookRotation(direction, Vector3.up) : fallback;
            return new Pose(position, rotation);
        }
    }
}
