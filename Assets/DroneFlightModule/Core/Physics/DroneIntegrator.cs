using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// Advances the true drone state with semi-implicit Euler: gravity, linear and angular drag,
    /// and ground contact against the terrain.
    /// </summary>
    public sealed class DroneIntegrator
    {
        public const float StandardGravity = 9.80665f;

        private const float TwoPi = 2f * MathF.PI;

        private readonly DroneConfig _config;
        private readonly ITerrain _terrain;
        private readonly float _gravity;

        public DroneIntegrator(DroneConfig config, ITerrain terrain, float gravity = StandardGravity)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            if (!(gravity >= 0f) || float.IsInfinity(gravity))
                throw new ArgumentOutOfRangeException(nameof(gravity), gravity, "Must be non-negative and finite.");
            _gravity = gravity;
        }

        public DroneState Step(in DroneState state, in ForceCommand command, float dt)
        {
            var weight = new Vec3(0f, -_config.Mass * _gravity, 0f);
            var drag = -_config.LinearDrag * state.Velocity;
            var acceleration = (command.Force + weight + drag) / _config.Mass;
            var velocity = state.Velocity + acceleration * dt;
            var position = state.Position + velocity * dt;

            float yawAcceleration = (command.YawTorque - _config.AngularDrag * state.YawRate) / _config.YawInertia;
            float yawRate = state.YawRate + yawAcceleration * dt;
            float yaw = WrapAngle(state.Yaw + yawRate * dt);

            float groundHeight = _terrain.HeightAt(position.X, position.Z);
            if (position.Y > groundHeight)
            {
                return new DroneState(position, velocity, yaw, yawRate, isGrounded: false);    
            }
            
            // Resting on the ground: no sinking, sliding or spinning until enough thrust lifts it off.
            // A drone that was already down keeps its spot; one arriving from the air lands where it got to.
            bool wasResting = state.IsGrounded
                || state.Position.Y <= _terrain.HeightAt(state.Position.X, state.Position.Z);
            var contactPoint = wasResting ? state.Position : position;
            float contactHeight = _terrain.HeightAt(contactPoint.X, contactPoint.Z);
            return new DroneState(contactPoint.WithY(contactHeight), Vec3.Zero, state.Yaw, 0f, isGrounded: true);
        }

        private float WrapAngle(float radians)
        {
            float wrapped = radians % TwoPi;
            if (wrapped > MathF.PI) wrapped -= TwoPi;
            else if (wrapped <= -MathF.PI) wrapped += TwoPi;
            return wrapped;
        }
    }
}
