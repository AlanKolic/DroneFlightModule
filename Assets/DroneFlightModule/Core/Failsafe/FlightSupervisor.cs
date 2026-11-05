using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// Decides each tick whether the pilot's command reaches the actuators. While the gate is open
    /// the pilot flies. When it closes in the air the drone brakes to a stop and holds for the hold
    /// duration, then descends straight down, flaring near the ground, until touchdown cuts the
    /// motors. Holding spends a hold
    /// budget that flying earns back at the same rate and landing refills, so a link that drops
    /// again soon after it returns resumes the hold where it left off instead of restarting it,
    /// and a flapping link cannot keep the drone in the air forever.
    /// </summary>
    public sealed class FlightSupervisor
    {
        private const double TimeTolerance = 1e-6;

        private readonly IFlightGate gate;
        private readonly FailsafeConfig config;
        private readonly HorizontalSpeedController horizontalSpeed;
        private readonly VerticalSpeedController verticalSpeed;
        private double holdBudget;

        public FlightSupervisor(
            IFlightGate gate,
            DroneConfig droneConfig,
            FailsafeConfig config,
            float gravity = DroneIntegrator.StandardGravity)
        {
            this.gate = gate ?? throw new ArgumentNullException(nameof(gate));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            horizontalSpeed = new HorizontalSpeedController(droneConfig, config.SpeedGain);
            verticalSpeed = new VerticalSpeedController(droneConfig, config.SpeedGain, gravity);
            holdBudget = config.HoldDuration;
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
                holdBudget = state.IsGrounded ? config.HoldDuration : Math.Min(config.HoldDuration, holdBudget + deltaTime);
                HoldTimeRemaining = 0;
                Mode = state.IsGrounded ? FlightMode.Grounded : FlightMode.Piloted;
                return pilotCommand;
            }

            if (state.IsGrounded)
            {
                holdBudget = config.HoldDuration;
                HoldTimeRemaining = 0;
                Mode = FlightMode.Grounded;
                return ForceCommand.None;
            }

            if (Mode == FlightMode.Grounded || Mode == FlightMode.Piloted)
            {
                Mode = FlightMode.Holding;
                HoldTimeRemaining = holdBudget;
            }

            if (Mode == FlightMode.Holding)
            {
                HoldTimeRemaining = Math.Max(0, HoldTimeRemaining - deltaTime);
                if (HoldTimeRemaining <= TimeTolerance)
                {
                    HoldTimeRemaining = 0;
                    Mode = FlightMode.Descending;
                }
                holdBudget = HoldTimeRemaining;
            }

            // Yaw torque is zero: angular drag stops any spin, and turning does not move the drone.
            float targetVerticalSpeed = Mode == FlightMode.Holding ? 0f : -SinkRate(altitude);
            float thrust = verticalSpeed.Thrust(targetVerticalSpeed, state.Velocity.Y);
            return new ForceCommand(horizontalSpeed.Force(Vec3.Zero, state.Velocity).WithY(thrust), 0f);
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
    }
}
