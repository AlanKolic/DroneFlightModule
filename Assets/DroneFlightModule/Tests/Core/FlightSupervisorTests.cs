using System;
using NUnit.Framework;

namespace DroneFlightModule.Core.Tests
{
    public class FlightSupervisorTests
    {
        private const float DeltaTime = TestConfigs.DeltaTime;
        private const float Gravity = DroneIntegrator.StandardGravity;
        private const float Tolerance = 1e-4f;

        private readonly DroneConfig drone = DroneConfig.Default;
        private readonly FailsafeConfig config = FailsafeConfig.Default;
        private readonly ForceCommand pilotCommand = new ForceCommand(new Vec3(1f, 20f, 2f), 0.3f);
        private readonly DroneState airborne = DroneState.At(new Vec3(0f, 5f, 0f));
        private readonly DroneState grounded = new DroneState(Vec3.Zero, Vec3.Zero, 0f, 0f, isGrounded: true);

        private ScriptedFlightGate gate;
        private FlightSupervisor supervisor;

        [SetUp]
        public void SetUp()
        {
            gate = new ScriptedFlightGate();
            supervisor = new FlightSupervisor(gate, drone, config);
        }

        private AltitudeReading Altitude(float heightAboveGround)
        {
            return new AltitudeReading(heightAboveGround, true, 0.0);
        }

        private ForceCommand Supervise(in DroneState state, float heightAboveGround = 5f)
        {
            return supervisor.Supervise(pilotCommand, state, Altitude(heightAboveGround), DeltaTime);
        }

        private void HoldUntilDescending()
        {
            gate.IsFlightAllowed = false;
            while (supervisor.Mode != FlightMode.Descending)
            {
                Supervise(airborne);
            }
        }

        // Thrust minus weight minus drag: zero means the drone keeps its current vertical speed.
        private float NetVerticalForce(in ForceCommand command, float verticalSpeed)
        {
            return command.Force.Y - drone.Mass * Gravity - drone.LinearDrag * verticalSpeed;
        }

        private DroneState Sinking(float verticalSpeed)
        {
            return new DroneState(new Vec3(0f, 5f, 0f), new Vec3(0f, verticalSpeed, 0f), 0f, 0f, isGrounded: false);
        }

        [Test]
        public void Constructor_RejectsThrustThatCannotLiftTheDrone()
        {
            var weak = new DroneConfig(
                mass: 2f,
                yawInertia: 0.05f,
                linearDrag: 1f,
                angularDrag: 0.2f,
                maxThrust: 2f * Gravity,
                maxLateralForce: 10f,
                maxYawTorque: 0.5f);

            Assert.Throws<ArgumentException>(() => new FlightSupervisor(gate, weak, config));
        }

        [Test]
        public void StartsGroundedWithNoCountdown()
        {
            Assert.That(supervisor.Mode, Is.EqualTo(FlightMode.Grounded));
            Assert.That(supervisor.HoldTimeRemaining, Is.EqualTo(0.0));
        }

        [Test]
        public void GateOpen_Airborne_PilotFlies()
        {
            var command = Supervise(airborne);

            Assert.That(supervisor.Mode, Is.EqualTo(FlightMode.Piloted));
            Assert.That(command.Force, Is.EqualTo(pilotCommand.Force));
            Assert.That(command.YawTorque, Is.EqualTo(pilotCommand.YawTorque));
        }

        [Test]
        public void GateOpen_Grounded_PilotCommandPassesForTakeoff()
        {
            var command = Supervise(grounded);

            Assert.That(supervisor.Mode, Is.EqualTo(FlightMode.Grounded));
            Assert.That(command.Force, Is.EqualTo(pilotCommand.Force));
        }

        [Test]
        public void GateClosed_Grounded_MotorsOffAndNoCountdown()
        {
            gate.IsFlightAllowed = false;

            var command = Supervise(grounded);

            Assert.That(supervisor.Mode, Is.EqualTo(FlightMode.Grounded));
            Assert.That(supervisor.HoldTimeRemaining, Is.EqualTo(0.0));
            Assert.That(command.Force, Is.EqualTo(Vec3.Zero));
            Assert.That(command.YawTorque, Is.EqualTo(0f));
        }

        [Test]
        public void GateClosed_Airborne_HoldsAndStartsCountdown()
        {
            gate.IsFlightAllowed = false;

            var command = Supervise(airborne);

            Assert.That(supervisor.Mode, Is.EqualTo(FlightMode.Holding));
            Assert.That(supervisor.HoldTimeRemaining, Is.EqualTo(config.HoldDuration - DeltaTime).Within(1e-6));
            Assert.That(NetVerticalForce(command, 0f), Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Holding_IgnoresPilotLateralAndYaw()
        {
            gate.IsFlightAllowed = false;

            var command = Supervise(airborne);

            Assert.That(command.Force.X, Is.EqualTo(0f));
            Assert.That(command.Force.Z, Is.EqualTo(0f));
            Assert.That(command.YawTorque, Is.EqualTo(0f));
        }

        [Test]
        public void Holding_PushesBackTowardsZeroVerticalSpeed()
        {
            gate.IsFlightAllowed = false;

            Assert.That(NetVerticalForce(Supervise(Sinking(-1f)), -1f), Is.GreaterThan(0f));
            Assert.That(NetVerticalForce(Supervise(Sinking(1f)), 1f), Is.LessThan(0f));
        }

        [Test]
        public void Countdown_RunsOnSimulationTime()
        {
            gate.IsFlightAllowed = false;

            supervisor.Supervise(pilotCommand, airborne, Altitude(5f), 0.5f);
            supervisor.Supervise(pilotCommand, airborne, Altitude(5f), 0.25f);

            Assert.That(supervisor.HoldTimeRemaining, Is.EqualTo(config.HoldDuration - 0.75f).Within(1e-6));
        }

        [Test]
        public void Holding_DescendsExactlyWhenHoldDurationHasPassed()
        {
            gate.IsFlightAllowed = false;
            int ticks = (int)MathF.Round(config.HoldDuration / DeltaTime);

            for (int i = 0; i < ticks - 1; i++)
            {
                Supervise(airborne);
            }
            Assert.That(supervisor.Mode, Is.EqualTo(FlightMode.Holding));

            Supervise(airborne);
            Assert.That(supervisor.Mode, Is.EqualTo(FlightMode.Descending));
            Assert.That(supervisor.HoldTimeRemaining, Is.EqualTo(0.0));
        }

        [Test]
        public void GateReopensDuringHold_PilotFliesAndCountdownResets()
        {
            gate.IsFlightAllowed = false;
            Supervise(airborne);
            Supervise(airborne);

            gate.IsFlightAllowed = true;
            var command = Supervise(airborne);
            Assert.That(supervisor.Mode, Is.EqualTo(FlightMode.Piloted));
            Assert.That(supervisor.HoldTimeRemaining, Is.EqualTo(0.0));
            Assert.That(command.Force, Is.EqualTo(pilotCommand.Force));

            gate.IsFlightAllowed = false;
            Supervise(airborne);
            Assert.That(supervisor.HoldTimeRemaining, Is.EqualTo(config.HoldDuration - DeltaTime).Within(1e-6));
        }

        [Test]
        public void GateReopensDuringDescent_PilotFlies()
        {
            HoldUntilDescending();

            gate.IsFlightAllowed = true;
            var command = Supervise(airborne);

            Assert.That(supervisor.Mode, Is.EqualTo(FlightMode.Piloted));
            Assert.That(command.Force, Is.EqualTo(pilotCommand.Force));
        }

        [Test]
        public void Descending_AboveFlareHeight_TargetsDescentSinkRate()
        {
            HoldUntilDescending();
            float sinkRate = -config.DescentSinkRate;

            var command = supervisor.Supervise(pilotCommand, Sinking(sinkRate), Altitude(config.FlareHeight + 3f), DeltaTime);

            Assert.That(NetVerticalForce(command, sinkRate), Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Descending_HalfwayThroughFlare_SlowsBetweenDescentAndTouchdownRates()
        {
            HoldUntilDescending();
            float sinkRate = -(config.TouchdownSinkRate + config.DescentSinkRate) / 2f;

            var command = supervisor.Supervise(pilotCommand, Sinking(sinkRate), Altitude(config.FlareHeight / 2f), DeltaTime);

            Assert.That(NetVerticalForce(command, sinkRate), Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Descending_AtGroundLevel_TargetsTouchdownSinkRate()
        {
            HoldUntilDescending();
            float sinkRate = -config.TouchdownSinkRate;

            var command = supervisor.Supervise(pilotCommand, Sinking(sinkRate), Altitude(0f), DeltaTime);

            Assert.That(NetVerticalForce(command, sinkRate), Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Descending_WithInvalidReading_TargetsFallbackSinkRate()
        {
            HoldUntilDescending();
            float sinkRate = -config.FallbackSinkRate;

            var command = supervisor.Supervise(pilotCommand, Sinking(sinkRate), AltitudeReading.Invalid(0.0), DeltaTime);

            Assert.That(NetVerticalForce(command, sinkRate), Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Descending_Touchdown_CutsMotors()
        {
            HoldUntilDescending();

            var command = Supervise(grounded);

            Assert.That(supervisor.Mode, Is.EqualTo(FlightMode.Grounded));
            Assert.That(command.Force, Is.EqualTo(Vec3.Zero));
        }

        [Test]
        public void Thrust_IsClampedToActuatorLimits()
        {
            gate.IsFlightAllowed = false;

            var fallingFast = Supervise(Sinking(-50f));
            var risingFast = Supervise(Sinking(50f));

            Assert.That(fallingFast.Force.Y, Is.EqualTo(drone.MaxThrust));
            Assert.That(risingFast.Force.Y, Is.EqualTo(0f));
        }
    }
}
