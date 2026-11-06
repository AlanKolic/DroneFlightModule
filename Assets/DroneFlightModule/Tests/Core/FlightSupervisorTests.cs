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
        private readonly DroneState airborne = new DroneState(new Vec3(0f, 5f, 0f));
        private readonly DroneState grounded = new DroneState(Vec3.Zero, Vec3.Zero, 0f, 0f, isGrounded: true);

        private ScriptedFlightGate gate;
        private ArmingSwitch arming;
        private FlightSupervisor supervisor;

        [SetUp]
        public void SetUp()
        {
            gate = new ScriptedFlightGate();
            arming = new ArmingSwitch();
            supervisor = new FlightSupervisor(gate, arming, drone, config);
        }

        private AltitudeReading Altitude(float heightAboveGround)
        {
            return new AltitudeReading(heightAboveGround, true);
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

            Assert.Throws<ArgumentException>(() => new FlightSupervisor(gate, arming, weak, config));
        }

        [Test]
        public void StartsDisarmedWithNoCountdown()
        {
            Assert.That(supervisor.Mode, Is.EqualTo(FlightMode.Disarmed));
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
        public void Armed_OnTheGround_PassesThePilotsCommand()
        {
            arming.Arm();

            var command = Supervise(grounded);

            Assert.That(supervisor.Mode, Is.EqualTo(FlightMode.Armed));
            Assert.That(command.Force, Is.EqualTo(pilotCommand.Force));
        }

        [Test]
        public void Disarmed_OnTheGround_IgnoresThePilot()
        {
            var command = Supervise(grounded);

            Assert.That(supervisor.Mode, Is.EqualTo(FlightMode.Disarmed));
            Assert.That(command.Force, Is.EqualTo(Vec3.Zero));
            Assert.That(command.YawTorque, Is.EqualTo(0f));
        }

        [Test]
        public void Touchdown_Disarms()
        {
            arming.Arm();
            Supervise(airborne);

            var command = Supervise(grounded);

            Assert.That(arming.IsArmed, Is.False);
            Assert.That(supervisor.Mode, Is.EqualTo(FlightMode.Disarmed));
            Assert.That(command.Force, Is.EqualTo(Vec3.Zero));
        }

        [Test]
        public void ArmedBeforeTakeoff_StaysArmed()
        {
            arming.Arm();

            Supervise(grounded);
            Supervise(grounded);

            Assert.That(arming.IsArmed, Is.True);
        }

        [Test]
        public void TheSwitch_IsIgnoredInTheAir()
        {
            var command = Supervise(airborne);

            Assert.That(arming.IsArmed, Is.False);
            Assert.That(supervisor.Mode, Is.EqualTo(FlightMode.Piloted));
            Assert.That(command.Force, Is.EqualTo(pilotCommand.Force));
        }

        [Test]
        public void AGateClosingOnTheGround_Disarms()
        {
            arming.Arm();
            gate.IsFlightAllowed = false;
            Supervise(grounded);
            gate.IsFlightAllowed = true;

            var command = Supervise(grounded);

            Assert.That(arming.IsArmed, Is.False);
            Assert.That(command.Force, Is.EqualTo(Vec3.Zero));
        }

        [Test]
        public void GateClosed_Grounded_MotorsOffAndNoCountdown()
        {
            gate.IsFlightAllowed = false;

            var command = Supervise(grounded);

            Assert.That(supervisor.Mode, Is.EqualTo(FlightMode.Disarmed));
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
        public void Holding_BrakesHorizontalDrift()
        {
            gate.IsFlightAllowed = false;
            var drifting = new DroneState(new Vec3(0f, 5f, 0f), new Vec3(8f, 0f, -3f), 0f, 0f, isGrounded: false);

            var command = Supervise(drifting);

            Assert.That(command.Force.X, Is.LessThan(0f));
            Assert.That(command.Force.Z, Is.GreaterThan(0f));
        }

        [Test]
        public void Holding_StopsADroneFlyingAtFullSpeedWithinAFewMetres()
        {
            gate.IsFlightAllowed = false;
            var integrator = new DroneIntegrator(drone, new FlatTerrain());
            var state = new DroneState(new Vec3(0f, 5f, 0f), new Vec3(8f, 0f, 0f), 0f, 0f, isGrounded: false);

            for (int i = 0; i < 150; i++)
            {
                state = integrator.Step(state, Supervise(state), DeltaTime);
            }

            Assert.That(supervisor.Mode, Is.EqualTo(FlightMode.Holding));
            Assert.That(state.Position.X, Is.LessThan(4f));
            Assert.That(Math.Abs(state.Velocity.X), Is.LessThan(0.01f));
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
        public void GateReopensDuringHold_PilotFliesAndTheHoldResumesWhereItLeftOff()
        {
            gate.IsFlightAllowed = false;
            Supervise(airborne);
            Supervise(airborne);

            gate.IsFlightAllowed = true;
            var command = Supervise(airborne);
            Assert.That(supervisor.Mode, Is.EqualTo(FlightMode.Piloted));
            Assert.That(supervisor.HoldTimeRemaining, Is.EqualTo(0.0));
            Assert.That(command.Force, Is.EqualTo(pilotCommand.Force));

            // Two ticks held, part of one earned back flying, one spent now.
            gate.IsFlightAllowed = false;
            Supervise(airborne);
            double expected = config.HoldDuration - (3.0 - config.HoldRecoveryRate) * DeltaTime;
            Assert.That(supervisor.HoldTimeRemaining, Is.EqualTo(expected).Within(1e-6));
        }

        [Test]
        public void FlyingForTheHoldDuration_EarnsBackOnlyPartOfTheHold()
        {
            HoldUntilDescending();

            gate.IsFlightAllowed = true;
            supervisor.Supervise(pilotCommand, airborne, Altitude(5f), config.HoldDuration);
            gate.IsFlightAllowed = false;
            Supervise(airborne);

            double expected = (double)config.HoldDuration * config.HoldRecoveryRate - DeltaTime;
            Assert.That(supervisor.HoldTimeRemaining, Is.EqualTo(expected).Within(1e-6));
        }

        [Test]
        public void FlyingLongEnough_EarnsBackTheFullHold()
        {
            HoldUntilDescending();

            gate.IsFlightAllowed = true;
            supervisor.Supervise(pilotCommand, airborne, Altitude(5f), config.HoldDuration / config.HoldRecoveryRate);
            gate.IsFlightAllowed = false;
            Supervise(airborne);

            Assert.That(supervisor.Mode, Is.EqualTo(FlightMode.Holding));
            Assert.That(supervisor.HoldTimeRemaining, Is.EqualTo(config.HoldDuration - DeltaTime).Within(1e-6));
        }

        [Test]
        public void FlappingLinkDuringDescent_KeepsDescending()
        {
            HoldUntilDescending();

            for (int i = 0; i < 100; i++)
            {
                gate.IsFlightAllowed = true;
                Supervise(airborne);
                gate.IsFlightAllowed = false;
                Supervise(airborne);
                Assert.That(supervisor.Mode, Is.EqualTo(FlightMode.Descending));
            }
        }

        [Test]
        public void LinkUpAndDownEverySecond_RunsTheHoldOut()
        {
            int ticksPerSecond = (int)MathF.Round(1f / DeltaTime);

            // Each cycle spends a second and earns back a quarter, so the ten seconds are gone by the 13th.
            for (int cycle = 0; cycle < 15; cycle++)
            {
                gate.IsFlightAllowed = true;
                for (int i = 0; i < ticksPerSecond; i++)
                {
                    Supervise(airborne);
                }
                gate.IsFlightAllowed = false;
                for (int i = 0; i < ticksPerSecond; i++)
                {
                    Supervise(airborne);
                }
            }

            Assert.That(supervisor.Mode, Is.EqualTo(FlightMode.Descending));
        }

        [Test]
        public void Landing_RefillsTheHold()
        {
            HoldUntilDescending();
            Supervise(grounded);

            gate.IsFlightAllowed = true;
            Supervise(airborne);
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

            var command = supervisor.Supervise(pilotCommand, Sinking(sinkRate), AltitudeReading.Invalid, DeltaTime);

            Assert.That(NetVerticalForce(command, sinkRate), Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Descending_Touchdown_CutsMotors()
        {
            HoldUntilDescending();

            var command = Supervise(grounded);

            Assert.That(supervisor.Mode, Is.EqualTo(FlightMode.Disarmed));
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
