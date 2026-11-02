using System;
using DroneFlightModule.Core;
using UnityEngine;

namespace DroneFlightModule.Unity
{
    /// <summary>
    /// Shows one drone by following its flight loop. Only reads from the loop and never feeds anything back.
    /// </summary>
    public sealed class DroneView : MonoBehaviour
    {
        private readonly StateInterpolator interpolator = new StateInterpolator();
        private FlightLoop loop;

        public void Bind(FlightLoop loop)
        {
            if (loop == null)
            {
                throw new ArgumentNullException(nameof(loop));
            }

            Unbind();
            this.loop = loop;
            interpolator.Reset(loop.State);
            loop.Ticked += OnTicked;
            Apply(1f);
        }

        private void Update()
        {
            if (loop == null)
            {
                return;
            }
            Apply(interpolator.BlendFactor(Time.time, Time.fixedTime, Time.fixedDeltaTime));
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Unbind()
        {
            if (loop == null)
            {
                return;
            }
            loop.Ticked -= OnTicked;
            loop = null;
        }

        private void OnTicked(FlightFrame frame)
        {
            interpolator.Push(frame.State);
        }

        private void Apply(float blend)
        {
            var pose = interpolator.Sample(blend);
            transform.SetPositionAndRotation(pose.position, pose.rotation);
        }
    }
}
