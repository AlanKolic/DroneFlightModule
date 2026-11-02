using DroneFlightModule.Core;
using UnityEngine;

namespace DroneFlightModule.Unity
{
    /// <summary>
    /// Smooths the fixed-step simulation for rendering by blending between the last two ticked
    /// states. The view runs up to one tick behind the simulation in exchange for no stutter.
    /// </summary>
    public sealed class StateInterpolator
    {
        private Pose previous = Pose.identity;
        private Pose current = Pose.identity;

        public void Reset(in DroneState state)
        {
            previous = state.ToPose();
            current = previous;
        }

        public void Push(in DroneState state)
        {
            previous = current;
            current = state.ToPose();
        }

        public Pose Sample(float blend)
        {
            blend = Mathf.Clamp01(blend);
            return new Pose(
                Vector3.Lerp(previous.position, current.position, blend),
                Quaternion.Slerp(previous.rotation, current.rotation, blend));
        }

        public float BlendFactor(float time, float fixedTime, float fixedDeltaTime)
        {
            if (!(fixedDeltaTime > 0f))
            {
                return 1f;
            }
            return Mathf.Clamp01((time - fixedTime) / fixedDeltaTime);
        }
    }
}
