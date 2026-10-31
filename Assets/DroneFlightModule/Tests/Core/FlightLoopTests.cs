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

        [SetUp]
        public void SetUp()
        {
            input = new ScriptedInputProvider();
        }

        private FlightLoop CreateLoop(DroneState initial, float gravity = DroneIntegrator.StandardGravity)
        {
            var config = DroneConfig.Default;
            var terrain = new FlatTerrain();
            return new FlightLoop(
                TestDrone,
                input,
                new ForceFlightController(config),
                new DroneIntegrator(config, terrain, gravity),
                new RangefinderAltitudeSensor(terrain, maxRange: 20f, updateRate: 1f / DeltaTime),
                initial);
        }

        private static void Run(FlightLoop loop, float seconds)
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
            var loop = CreateLoop(DroneState.At(Vec3.Zero));

            Assert.Throws<ArgumentOutOfRangeException>(() => loop.Tick(deltaTime));
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
                new DroneIntegrator(config, terrain),
                new RangefinderAltitudeSensor(terrain),
                DroneState.At(Vec3.Zero)));
        }

        [Test]
        public void Constructor_RequiresAltitudeSensor()
        {
            var config = DroneConfig.Default;

            Assert.Throws<ArgumentNullException>(() => new FlightLoop(
                TestDrone,
                input,
                new ForceFlightController(config),
                new DroneIntegrator(config, new FlatTerrain()),
                null,
                DroneState.At(Vec3.Zero)));
        }

        [Test]
        public void NoInput_FallsAndLands()
        {
            var loop = CreateLoop(DroneState.At(new Vec3(0f, 10f, 0f)));

            Run(loop, 5f);

            Assert.That(loop.State.IsGrounded, Is.True);
            Assert.That(loop.State.Position.Y, Is.EqualTo(0f));
        }

        [Test]
        public void FullClimb_TakesOffFromGround()
        {
            var loop = CreateLoop(DroneState.At(Vec3.Zero));
            input.Current = new ControlInput(Vec3.Up, 0f);

            Run(loop, 1f);

            Assert.That(loop.State.IsGrounded, Is.False);
            Assert.That(loop.State.Position.Y, Is.GreaterThan(1f));
        }

        [Test]
        public void Forward_FliesAlongHeading()
        {
            var facingRight = DroneState.At(new Vec3(0f, 10f, 0f), yaw: MathF.PI / 2f);
            var loop = CreateLoop(facingRight, gravity: 0f);
            input.Current = new ControlInput(Vec3.Forward, 0f);

            Run(loop, 2f);

            Assert.That(loop.State.Position.X, Is.GreaterThan(1f));
            Assert.That(loop.State.Position.Z, Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void Ticked_PublishesOneFrameMatchingStatePerTick()
        {
            var loop = CreateLoop(DroneState.At(new Vec3(0f, 10f, 0f)));
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
            var loop = CreateLoop(DroneState.At(Vec3.Zero));
            var frames = new List<FlightFrame>();
            loop.Ticked += frames.Add;

            loop.Tick(DeltaTime);
            input.Current = new ControlInput(Vec3.Up, 0f);
            loop.Tick(DeltaTime);

            Assert.That(frames[0].Command.Force, Is.EqualTo(Vec3.Zero));
            Assert.That(frames[1].Command.Force.Y, Is.EqualTo(DroneConfig.Default.MaxThrust));
        }

        [Test]
        public void Ticked_CarriesAltitudeSampledBeforeTheStep()
        {
            var loop = CreateLoop(DroneState.At(new Vec3(0f, 10f, 0f)));
            var frames = new List<FlightFrame>();
            loop.Ticked += frames.Add;

            Run(loop, 2 * DeltaTime);

            Assert.That(frames[0].Altitude.IsValid, Is.True);
            Assert.That(frames[0].Altitude.HeightAboveGround, Is.EqualTo(10f));
            Assert.That(frames[0].Altitude.Timestamp, Is.EqualTo(0.0));
            Assert.That(frames[1].Altitude.HeightAboveGround, Is.EqualTo(frames[0].State.Position.Y));
        }
    }
}
