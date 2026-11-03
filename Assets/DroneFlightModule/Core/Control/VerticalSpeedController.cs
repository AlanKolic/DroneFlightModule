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
        private readonly DroneConfig droneConfig;
        private readonly float gain;
        private readonly float gravity;

        public VerticalSpeedController(DroneConfig droneConfig, float gain, float gravity)
        {
            this.droneConfig = droneConfig ?? throw new ArgumentNullException(nameof(droneConfig));
            this.gain = guard.RequirePositive(gain, nameof(gain));
            this.gravity = guard.RequireNonNegative(gravity, nameof(gravity));
            if (!(droneConfig.MaxThrust > droneConfig.Mass * this.gravity))
            {
                throw new ArgumentException("Max thrust must exceed the drone's weight, or it cannot hover.", nameof(droneConfig));
            }
        }

        public float Thrust(float targetVerticalSpeed, float verticalSpeed)
        {
            float thrust = droneConfig.Mass * gravity
                + droneConfig.LinearDrag * verticalSpeed
                + droneConfig.Mass * gain * (targetVerticalSpeed - verticalSpeed);
            return Math.Clamp(thrust, 0f, droneConfig.MaxThrust);
        }
    }
}
