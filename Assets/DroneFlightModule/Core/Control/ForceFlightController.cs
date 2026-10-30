using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// Simple force model: input axes scale the frame's force limits, rotated into world space
    /// by the drone's yaw. There is no gravity compensation, so the drone must be pushed up.
    /// </summary>
    public sealed class ForceFlightController : IFlightController
    {
        private readonly DroneConfig _config;

        public ForceFlightController(DroneConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public ForceCommand Compute(in ControlInput input, in DroneState state)
        {
            float thrust = MathF.Max(0f, input.Move.Y) * _config.MaxThrust;
            var localForce = new Vec3(
                input.Move.X * _config.MaxLateralForce,
                thrust,
                input.Move.Z * _config.MaxLateralForce);

            return new ForceCommand(localForce.RotateYaw(state.Yaw), input.Yaw * _config.MaxYawTorque);
        }
    }
}
