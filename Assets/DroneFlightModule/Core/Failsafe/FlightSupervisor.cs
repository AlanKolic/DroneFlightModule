using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// Decides each tick whether the pilot's command reaches the actuators. While the gate is open
    /// the pilot flies. When it closes in the air the drone holds for the hold duration, then
    /// descends, flaring near the ground, until touchdown cuts the motors.
    /// </summary>
    public sealed class FlightSupervisor
    {
        private const double TimeTolerance = 1e-6;

        private readonly IFlightGate gate;
        private readonly DroneConfig drone;
        private readonly FailsafeConfig config;
        private readonly float gravity;

        public FlightSupervisor(
            IFlightGate gate,
            DroneConfig drone,
            FailsafeConfig config,
            float gravity = DroneIntegrator.StandardGravity)
        {
            this.gate = gate ?? throw new ArgumentNullException(nameof(gate));
            this.drone = drone ?? throw new ArgumentNullException(nameof(drone));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            if (!(gravity >= 0f) || float.IsInfinity(gravity))
            {
                throw new ArgumentOutOfRangeException(nameof(gravity), gravity, "Must be non-negative and finite.");
            }
            if (!(drone.MaxThrust > drone.Mass * gravity))
            {
                throw new ArgumentException("Max thrust must exceed the drone's weight, or it cannot hold or descend slowly.", nameof(drone));
            }
            this.gravity = gravity;
        }

        public FlightMode Mode { get; private set; } = FlightMode.Grounded;
        public double HoldTimeRemaining { get; private set; }

        public ForceCommand Supervise(
            in ForceCommand pilotCommand,
            in DroneState state,
            in AltitudeReading altitude,
            float deltaTime)
        {
            if (gate.IsFlightAllowed)
            {
                HoldTimeRemaining = 0;
                Mode = state.IsGrounded ? FlightMode.Grounded : FlightMode.Piloted;
                return pilotCommand;
            }

            if (state.IsGrounded)
            {
                HoldTimeRemaining = 0;
                Mode = FlightMode.Grounded;
                return ForceCommand.None;
            }

            if (Mode == FlightMode.Grounded || Mode == FlightMode.Piloted)
            {
                Mode = FlightMode.Holding;
                HoldTimeRemaining = config.HoldDuration;
            }

            if (Mode == FlightMode.Holding)
            {
                HoldTimeRemaining = Math.Max(0, HoldTimeRemaining - deltaTime);
                if (HoldTimeRemaining <= TimeTolerance)
                {
                    HoldTimeRemaining = 0;
                    Mode = FlightMode.Descending;
                }
            }

            float targetVerticalSpeed = Mode == FlightMode.Holding ? 0f : -SinkRate(altitude);
            return VerticalSpeedCommand(targetVerticalSpeed, state);
        }

        private float SinkRate(in AltitudeReading altitude)
        {
            if (!altitude.IsValid)
            {
                return config.FallbackSinkRate;
            }
            float flare = Math.Clamp(altitude.HeightAboveGround / config.FlareHeight, 0f, 1f);
            return config.TouchdownSinkRate + (config.DescentSinkRate - config.TouchdownSinkRate) * flare;
        }

        // Weight and vertical drag are cancelled, so the drone settles exactly on the target speed.
        // Lateral force and yaw torque are zero; drag brings any drift to rest.
        private ForceCommand VerticalSpeedCommand(float targetVerticalSpeed, in DroneState state)
        {
            float verticalSpeed = state.Velocity.Y;
            float thrust = drone.Mass * gravity
                + drone.LinearDrag * verticalSpeed
                + drone.Mass * config.VerticalSpeedGain * (targetVerticalSpeed - verticalSpeed);
            thrust = Math.Clamp(thrust, 0f, drone.MaxThrust);
            return new ForceCommand(new Vec3(0f, thrust, 0f), 0f);
        }
    }
}
