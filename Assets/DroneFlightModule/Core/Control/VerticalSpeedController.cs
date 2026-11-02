using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// Thrust that drives the drone towards a target vertical speed. Weight and vertical drag are
    /// cancelled, so the drone settles exactly on the target; the proportional term closes the gap.
    /// </summary>
    public sealed class VerticalSpeedController
    {
        private readonly ArgumentGuard guard = new ArgumentGuard();
        private readonly DroneConfig drone;
        private readonly float gain;
        private readonly float gravity;

        public VerticalSpeedController(DroneConfig drone, float gain, float gravity)
        {
            this.drone = drone ?? throw new ArgumentNullException(nameof(drone));
            this.gain = guard.RequirePositive(gain, nameof(gain));
            this.gravity = guard.RequireNonNegative(gravity, nameof(gravity));
            if (!(drone.MaxThrust > drone.Mass * this.gravity))
            {
                throw new ArgumentException("Max thrust must exceed the drone's weight, or it cannot hover.", nameof(drone));
            }
        }

        public float Thrust(float targetVerticalSpeed, float verticalSpeed)
        {
            float thrust = drone.Mass * gravity
                + drone.LinearDrag * verticalSpeed
                + drone.Mass * gain * (targetVerticalSpeed - verticalSpeed);
            return Math.Clamp(thrust, 0f, drone.MaxThrust);
        }
    }
}
