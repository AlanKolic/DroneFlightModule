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

        private readonly DroneConfig config;
        private readonly ITerrain terrain;
        private readonly float gravity;

        public DroneIntegrator(DroneConfig config, ITerrain terrain, float gravity = StandardGravity)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            if (!(gravity >= 0f) || float.IsInfinity(gravity))
            {
                throw new ArgumentOutOfRangeException(nameof(gravity), gravity, "Must be non-negative and finite.");
            }
            this.gravity = gravity;
        }

        public DroneState Step(in DroneState state, in ForceCommand command, float deltaTime)
        {
            var weight = new Vec3(0f, -config.Mass * gravity, 0f);
            var drag = -config.LinearDrag * state.Velocity;
            var acceleration = (command.Force + weight + drag) / config.Mass;
            var velocity = state.Velocity + acceleration * deltaTime;
            var position = state.Position + velocity * deltaTime;

            float yawAcceleration = (command.YawTorque - config.AngularDrag * state.YawRate) / config.YawInertia;
            float yawRate = state.YawRate + yawAcceleration * deltaTime;
            float yaw = WrapAngle(state.Yaw + yawRate * deltaTime);

            float groundHeight = terrain.HeightAt(position.X, position.Z);
            if (position.Y > groundHeight)
            {
                return new DroneState(position, velocity, yaw, yawRate, isGrounded: false);    
            }
            
            // Resting on the ground: no sinking, sliding or spinning until enough thrust lifts it off.
            // A drone that was already down keeps its spot; one arriving from the air lands where it got to.
            bool wasResting = state.IsGrounded
                || state.Position.Y <= terrain.HeightAt(state.Position.X, state.Position.Z);
            var contactPoint = wasResting ? state.Position : position;
            float contactHeight = terrain.HeightAt(contactPoint.X, contactPoint.Z);
            return new DroneState(contactPoint.WithY(contactHeight), Vec3.Zero, state.Yaw, 0f, isGrounded: true);
        }

        private float WrapAngle(float radians)
        {
            float wrapped = radians % TwoPi;
            if (wrapped > MathF.PI)
            {
                wrapped -= TwoPi;
            }
            else if (wrapped <= -MathF.PI)
            {
                wrapped += TwoPi;
            }
            return wrapped;
        }
    }
}
