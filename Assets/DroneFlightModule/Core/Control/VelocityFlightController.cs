using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// Stick input sets target velocities: move in the drone's heading frame, climb rate and yaw
    /// rate. Centred sticks hold position and altitude, so the drone hovers hands-off. Each axis is
    /// a proportional controller that also cancels drag, limited by the frame's force and torque.
    /// </summary>
    public sealed class VelocityFlightController : IFlightController
    {
        private readonly DroneConfig droneConfig;
        private readonly VelocityControllerConfig config;
        private readonly VerticalSpeedController verticalSpeed;

        public VelocityFlightController(
            DroneConfig droneConfig,
            VelocityControllerConfig config,
            float gravity = DroneIntegrator.StandardGravity)
        {
            this.droneConfig = droneConfig ?? throw new ArgumentNullException(nameof(droneConfig));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            verticalSpeed = new VerticalSpeedController(droneConfig, config.VerticalSpeedGain, gravity);
        }

        public ForceCommand Compute(in ControlInput input, in DroneState state)
        {
            var targetVelocity = new Vec3(
                input.Move.X * config.MaxHorizontalSpeed,
                input.Move.Y * config.MaxClimbRate,
                input.Move.Z * config.MaxHorizontalSpeed).RotateYaw(state.Yaw);

            var velocity = state.Velocity;
            var horizontalForce = new Vec3(
                HorizontalForce(targetVelocity.X, velocity.X),
                0f,
                HorizontalForce(targetVelocity.Z, velocity.Z));
            float horizontalMagnitude = horizontalForce.Magnitude;
            if (horizontalMagnitude > droneConfig.MaxLateralForce)
            {
                horizontalForce *= droneConfig.MaxLateralForce / horizontalMagnitude;
            }

            float thrust = verticalSpeed.Thrust(targetVelocity.Y, velocity.Y);
            return new ForceCommand(horizontalForce.WithY(thrust), YawTorque(input.Yaw * config.MaxYawRate, state.YawRate));
        }

        private float HorizontalForce(float targetSpeed, float speed)
        {
            return droneConfig.LinearDrag * speed + droneConfig.Mass * config.HorizontalSpeedGain * (targetSpeed - speed);
        }

        private float YawTorque(float targetYawRate, float yawRate)
        {
            float torque = droneConfig.AngularDrag * yawRate + droneConfig.YawInertia * config.YawRateGain * (targetYawRate - yawRate);
            return Math.Clamp(torque, -droneConfig.MaxYawTorque, droneConfig.MaxYawTorque);
        }
    }
}
