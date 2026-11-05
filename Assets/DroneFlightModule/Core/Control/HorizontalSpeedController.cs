using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// Lateral force that drives the drone towards a target horizontal velocity in world space.
    /// Drag is cancelled, so the drone settles exactly on the target; the proportional term closes
    /// the gap, and the force is limited by the frame's lateral force.
    /// </summary>
    public sealed class HorizontalSpeedController
    {
        private readonly ArgumentGuard guard = new ArgumentGuard();
        private readonly DroneConfig droneConfig;
        private readonly float gain;

        public HorizontalSpeedController(DroneConfig droneConfig, float gain)
        {
            this.droneConfig = droneConfig ?? throw new ArgumentNullException(nameof(droneConfig));
            this.gain = guard.RequirePositive(gain, nameof(gain));
        }

        public Vec3 Force(in Vec3 targetVelocity, in Vec3 velocity)
        {
            var force = new Vec3(Axis(targetVelocity.X, velocity.X), 0f, Axis(targetVelocity.Z, velocity.Z));
            float magnitude = force.Magnitude;
            if (magnitude > droneConfig.MaxLateralForce)
            {
                force *= droneConfig.MaxLateralForce / magnitude;
            }
            return force;
        }

        private float Axis(float targetSpeed, float speed)
        {
            return droneConfig.LinearDrag * speed + droneConfig.Mass * gain * (targetSpeed - speed);
        }
    }
}
