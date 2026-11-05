using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DroneFlightModule.Core.Tests
{
    public class FlightLoopTests
    {
        private const float DeltaTime = TestConfigs.DeltaTime;

        private static readonly DroneId TestDrone = new DroneId("drone-1");

        private ScriptedInputProvider input;
        private ScriptedFlightGate gate;

        [SetUp]
        public void SetUp()
        {
            input = new ScriptedInputProvider();
            gate = new ScriptedFlightGate();
        }

        private FlightLoop CreateLoop(
            DroneState initial,
            float gravity = DroneIntegrator.StandardGravity,
            IInputProvider inputProvider = null,
            IFlightGate flightGate = null)
        {
            var config = DroneConfig.Default;
            var terrain = new FlatTerrain();
            return new FlightLoop(
                TestDrone,
                inputProvider ?? input,
                new ForceFlightController(config),
                new FlightSupervisor(flightGate ?? gate, config, FailsafeConfig.Default, gravity),
                new DroneIntegrator(config, terrain, gravity),
                new RangefinderAltitudeSensor(terrain, new RangefinderConfig(maxRange: 20f, updateRate: 1f / DeltaTime)),
                initial);
        }

        private void Run(FlightLoop loop, float seconds)
        {
            int ticks = (int)MathF.Round(seconds / DeltaTime);
            for (int i = 0; i < ticks; i++)
            {
                loop.Tick(DeltaTime);
            }
        }

        [TestCase(0f)]
        [TestCase(-0.02f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Tick_RejectsInvalidDuration(float deltaTime)
        {
            var loop = CreateLoop(new DroneState(Vec3.Zero));

            Assert.Throws<ArgumentOutOfRangeException>(() => loop.Tick(deltaTime));
        }

        [Test]
        public void Time_StartsAtTheGivenStartTime_AndAdvancesByEachStep()
        {
            var config = DroneConfig.Default;
            var terrain = new FlatTerrain();
            var loop = new FlightLoop(
                TestDrone,
                input,
                new ForceFlightController(config),
                new FlightSupervisor(gate, config, FailsafeConfig.Default),
                new DroneIntegrator(config, terrain),
                new RangefinderAltitudeSensor(terrain, RangefinderConfig.Default),
                new DroneState(Vec3.Zero),
                startTime: 12.5);
            double ticked = -1.0;
            loop.Ticked += frame => ticked = frame.Time;

            loop.Tick(DeltaTime);

            Assert.That(loop.Time, Is.EqualTo(12.5 + DeltaTime).Within(1e-6));
            Assert.That(ticked, Is.EqualTo(loop.Time));
        }

        [TestCase(-1.0)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void Constructor_RejectsAnInvalidStartTime(double startTime)
        {
            var config = DroneConfig.Default;
            var terrain = new FlatTerrain();

            Assert.Throws<ArgumentOutOfRangeException>(() => new FlightLoop(
                TestDrone,
                input,
                new ForceFlightController(config),
                new FlightSupervisor(gate, config, FailsafeConfig.Default),
                new DroneIntegrator(config, terrain),
                new RangefinderAltitudeSensor(terrain, RangefinderConfig.Default),
                new DroneState(Vec3.Zero),
                startTime));
        }

        [Test]
        public void Constructor_RequiresDroneId()
        {
            var config = DroneConfig.Default;
            var terrain = new FlatTerrain();

            Assert.Throws<ArgumentException>(() => new FlightLoop(
                default,
                input,
                new ForceFlightController(config),
                new FlightSupervisor(gate, config, FailsafeConfig.Default),
                new DroneIntegrator(config, terrain),
                new RangefinderAltitudeSensor(terrain, RangefinderConfig.Default),
                new DroneState(Vec3.Zero)));
        }

        [Test]
        public void Constructor_RequiresAltitudeSensor()
        {
            var config = DroneConfig.Default;

            Assert.Throws<ArgumentNullException>(() => new FlightLoop(
                TestDrone,
                input,
                new ForceFlightController(config),
                new FlightSupervisor(gate, config, FailsafeConfig.Default),
                new DroneIntegrator(config, new FlatTerrain()),
                null,
                new DroneState(Vec3.Zero)));
        }

        [Test]
        public void Constructor_RequiresSupervisor()
        {
            var config = DroneConfig.Default;
            var terrain = new FlatTerrain();

            Assert.Throws<ArgumentNullException>(() => new FlightLoop(
                TestDrone,
                input,
                new ForceFlightController(config),
                null,
                new DroneIntegrator(config, terrain),
                new RangefinderAltitudeSensor(terrain, RangefinderConfig.Default),
                new DroneState(Vec3.Zero)));
        }

        [Test]
        public void NoInput_FallsAndLands()
        {
            var loop = CreateLoop(new DroneState(new Vec3(0f, 10f, 0f)));

            Run(loop, 5f);

            Assert.That(loop.State.IsGrounded, Is.True);
            Assert.That(loop.State.Position.Y, Is.EqualTo(0f));
        }

        [Test]
        public void FullClimb_TakesOffFromGround()
        {
            var loop = CreateLoop(new DroneState(Vec3.Zero));
            input.Current = new ControlInput(Vec3.Up, 0f);

            Run(loop, 1f);

            Assert.That(loop.State.IsGrounded, Is.False);
            Assert.That(loop.State.Position.Y, Is.GreaterThan(1f));
        }

        [Test]
        public void Forward_FliesAlongHeading()
        {
            var facingRight = new DroneState(new Vec3(0f, 10f, 0f), yaw: MathF.PI / 2f);
            var loop = CreateLoop(facingRight, gravity: 0f);
            input.Current = new ControlInput(Vec3.Forward, 0f);

            Run(loop, 2f);

            Assert.That(loop.State.Position.X, Is.GreaterThan(1f));
            Assert.That(loop.State.Position.Z, Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void Ticked_PublishesOneFrameMatchingStatePerTick()
        {
            var loop = CreateLoop(new DroneState(new Vec3(0f, 10f, 0f)));
            var frames = new List<FlightFrame>();
            loop.Ticked += frames.Add;

            Run(loop, 3 * DeltaTime);

            Assert.That(frames, Has.Count.EqualTo(3));
            Assert.That(frames[2].DroneId, Is.EqualTo(TestDrone));
            Assert.That(frames[2].Time, Is.EqualTo(3 * DeltaTime).Within(1e-6));
            Assert.That(frames[2].State.Position, Is.EqualTo(loop.State.Position));
        }

        [Test]
        public void InputChange_TakesEffectOnNextTick()
        {
            var loop = CreateLoop(new DroneState(Vec3.Zero));
            var frames = new List<FlightFrame>();
            loop.Ticked += frames.Add;

            loop.Tick(DeltaTime);
            input.Current = new ControlInput(Vec3.Up, 0f);
            loop.Tick(DeltaTime);

            Assert.That(frames[0].Command.Force, Is.EqualTo(Vec3.Zero));
            Assert.That(frames[1].Command.Force.Y, Is.EqualTo(DroneConfig.Default.MaxThrust));
        }

        [Test]
        public void ThroughRouter_OnlyAssignedPilotCanTakeOff()
        {
            var router = new InputRouter();
            var loop = CreateLoop(
                new DroneState(Vec3.Zero),
                inputProvider: router,
                flightGate: new PilotInControlGate(router));
            input.Current = new ControlInput(Vec3.Up, 0f);

            Run(loop, 0.5f);
            Assert.That(loop.State.IsGrounded, Is.True);

            router.Assign(new PilotId("pilot1"), input);
            Run(loop, 0.5f);
            Assert.That(loop.State.IsGrounded, Is.False);
        }

        [Test]
        public void Ticked_CarriesAltitudeSampledBeforeTheStep()
        {
            var loop = CreateLoop(new DroneState(new Vec3(0f, 10f, 0f)));
            var frames = new List<FlightFrame>();
            loop.Ticked += frames.Add;

            Run(loop, 2 * DeltaTime);

            Assert.That(frames[0].Altitude.IsValid, Is.True);
            Assert.That(frames[0].Altitude.HeightAboveGround, Is.EqualTo(10f));
            Assert.That(frames[1].Altitude.HeightAboveGround, Is.EqualTo(frames[0].State.Position.Y));
        }

        [Test]
        public void GateClosedOnGround_CannotTakeOff()
        {
            var loop = CreateLoop(new DroneState(Vec3.Zero));
            gate.IsFlightAllowed = false;
            input.Current = new ControlInput(Vec3.Up, 0f);

            Run(loop, 2f);

            Assert.That(loop.State.IsGrounded, Is.True);
            Assert.That(loop.Mode, Is.EqualTo(FlightMode.Grounded));
        }

        [Test]
        public void GateClosesInFlight_HoldsAltitudeForHoldDuration()
        {
            var loop = CreateLoop(new DroneState(new Vec3(0f, 5f, 0f)));
            gate.IsFlightAllowed = false;

            Run(loop, FailsafeConfig.Default.HoldDuration - 1f);

            Assert.That(loop.Mode, Is.EqualTo(FlightMode.Holding));
            Assert.That(loop.State.Position.Y, Is.EqualTo(5f).Within(0.01f));
        }

        [Test]
        public void GateClosesInFlight_DescendsAfterHoldAndLandsGently()
        {
            var loop = CreateLoop(new DroneState(new Vec3(0f, 5f, 0f)));
            gate.IsFlightAllowed = false;
            var frames = new List<FlightFrame>();
            loop.Ticked += frames.Add;

            Run(loop, FailsafeConfig.Default.HoldDuration);
            Assert.That(loop.Mode, Is.EqualTo(FlightMode.Descending));

            Run(loop, 30f);
            Assert.That(loop.State.IsGrounded, Is.True);
            Assert.That(loop.Mode, Is.EqualTo(FlightMode.Grounded));

            var lastAirborne = frames.FindLast(frame => !frame.State.IsGrounded);
            Assert.That(lastAirborne.State.Velocity.Y, Is.GreaterThan(-FailsafeConfig.Default.TouchdownSinkRate - 0.05f));
        }

        [Test]
        public void Descending_HighUp_SinksAtDescentRate()
        {
            var loop = CreateLoop(new DroneState(new Vec3(0f, 8f, 0f)));
            gate.IsFlightAllowed = false;

            Run(loop, FailsafeConfig.Default.HoldDuration + 2f);

            Assert.That(loop.Mode, Is.EqualTo(FlightMode.Descending));
            Assert.That(loop.State.Velocity.Y, Is.EqualTo(-FailsafeConfig.Default.DescentSinkRate).Within(0.05f));
        }

        [Test]
        public void GateReopensDuringHold_PilotFliesAgain()
        {
            var loop = CreateLoop(new DroneState(new Vec3(0f, 5f, 0f)));
            gate.IsFlightAllowed = false;
            input.Current = new ControlInput(Vec3.Up, 0f);
            Run(loop, 1f);
            Assert.That(loop.State.Position.Y, Is.EqualTo(5f).Within(0.01f));

            gate.IsFlightAllowed = true;
            Run(loop, 1f);

            Assert.That(loop.Mode, Is.EqualTo(FlightMode.Piloted));
            Assert.That(loop.State.Position.Y, Is.GreaterThan(6f));
        }

        [Test]
        public void Ticked_CarriesModeAndHoldTimeRemaining()
        {
            var loop = CreateLoop(new DroneState(new Vec3(0f, 5f, 0f)));
            var frames = new List<FlightFrame>();
            loop.Ticked += frames.Add;

            loop.Tick(DeltaTime);
            gate.IsFlightAllowed = false;
            loop.Tick(DeltaTime);

            Assert.That(frames[0].Mode, Is.EqualTo(FlightMode.Piloted));
            Assert.That(frames[0].HoldTimeRemaining, Is.EqualTo(0.0));
            Assert.That(frames[1].Mode, Is.EqualTo(FlightMode.Holding));
            Assert.That(frames[1].HoldTimeRemaining, Is.EqualTo(FailsafeConfig.Default.HoldDuration - DeltaTime).Within(1e-6));
        }
    }
}
