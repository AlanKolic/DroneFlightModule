using System;
using NUnit.Framework;

namespace DroneFlightModule.Core.Tests
{
    public class VelocityFlightControllerTests
    {
        private const float DeltaTime = TestConfigs.DeltaTime;
        private const float Gravity = DroneIntegrator.StandardGravity;
        private const float Tolerance = 1e-4f;

        private readonly DroneConfig drone = DroneConfig.Default;
        private readonly VelocityControllerConfig config = VelocityControllerConfig.Default;

        private VelocityFlightController controller;
        private DroneIntegrator integrator;

        [SetUp]
        public void SetUp()
        {
            controller = new VelocityFlightController(drone, config);
            integrator = new DroneIntegrator(drone, new FlatTerrain());
        }

        private DroneState Fly(DroneState state, ControlInput input, float seconds)
        {
            int ticks = (int)MathF.Round(seconds / DeltaTime);
            for (int i = 0; i < ticks; i++)
            {
                state = integrator.Step(state, controller.Compute(input, state), DeltaTime);
            }
            return state;
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

            Assert.Throws<ArgumentException>(() => new VelocityFlightController(weak, config));
        }

        [Test]
        public void CentredSticks_AtRest_ThrustCancelsWeightExactly()
        {
            var command = controller.Compute(ControlInput.Zero, DroneState.At(new Vec3(0f, 5f, 0f)));

            Assert.That(command.Force.Y, Is.EqualTo(drone.Mass * Gravity).Within(Tolerance));
            Assert.That(command.Force.X, Is.EqualTo(0f));
            Assert.That(command.Force.Z, Is.EqualTo(0f));
            Assert.That(command.YawTorque, Is.EqualTo(0f));
        }

        [Test]
        public void CentredSticks_HoverHandsOff()
        {
            var state = Fly(DroneState.At(new Vec3(0f, 5f, 0f)), ControlInput.Zero, 10f);

            Assert.That(state.Position.Y, Is.EqualTo(5f).Within(1e-3f));
            Assert.That(state.Velocity.Magnitude, Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void CentredSticks_BrakeExistingDrift()
        {
            var drifting = new DroneState(new Vec3(0f, 5f, 0f), new Vec3(4f, -2f, 3f), 0f, 1f, isGrounded: false);

            var state = Fly(drifting, ControlInput.Zero, 5f);

            Assert.That(state.Velocity.Magnitude, Is.EqualTo(0f).Within(0.01f));
            Assert.That(state.YawRate, Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public void CentredSticks_OnGround_StayGrounded()
        {
            var grounded = new DroneState(Vec3.Zero, Vec3.Zero, 0f, 0f, isGrounded: true);

            var state = Fly(grounded, ControlInput.Zero, 2f);

            Assert.That(state.IsGrounded, Is.True);
        }

        [Test]
        public void FullClimb_ReachesMaxClimbRate()
        {
            var grounded = new DroneState(Vec3.Zero, Vec3.Zero, 0f, 0f, isGrounded: true);

            var state = Fly(grounded, new ControlInput(Vec3.Up, 0f), 3f);

            Assert.That(state.IsGrounded, Is.False);
            Assert.That(state.Velocity.Y, Is.EqualTo(config.MaxClimbRate).Within(0.01f));
        }

        [Test]
        public void FullDescent_SinksAtMaxClimbRateAndLands()
        {
            var state = Fly(DroneState.At(new Vec3(0f, 10f, 0f)), new ControlInput(-Vec3.Up, 0f), 2f);
            Assert.That(state.Velocity.Y, Is.EqualTo(-config.MaxClimbRate).Within(0.01f));

            state = Fly(state, new ControlInput(-Vec3.Up, 0f), 5f);
            Assert.That(state.IsGrounded, Is.True);
        }

        [Test]
        public void FullForward_ReachesMaxHorizontalSpeedAlongHeading()
        {
            var facingRight = DroneState.At(new Vec3(0f, 5f, 0f), yaw: MathF.PI / 2f);

            var state = Fly(facingRight, new ControlInput(Vec3.Forward, 0f), 5f);

            Assert.That(state.Velocity.X, Is.EqualTo(config.MaxHorizontalSpeed).Within(0.05f));
            Assert.That(state.Velocity.Z, Is.EqualTo(0f).Within(0.01f));
            Assert.That(state.Position.Y, Is.EqualTo(5f).Within(0.01f));
        }

        [Test]
        public void FullYaw_ReachesMaxYawRate()
        {
            var state = Fly(DroneState.At(new Vec3(0f, 5f, 0f)), new ControlInput(Vec3.Zero, 1f), 2f);

            Assert.That(state.YawRate, Is.EqualTo(config.MaxYawRate).Within(0.01f));
        }

        [Test]
        public void HorizontalForce_IsLimitedToMaxLateralForce()
        {
            var command = controller.Compute(new ControlInput(new Vec3(1f, 0f, 1f), 0f), DroneState.At(new Vec3(0f, 5f, 0f)));

            float horizontal = new Vec3(command.Force.X, 0f, command.Force.Z).Magnitude;
            Assert.That(horizontal, Is.EqualTo(drone.MaxLateralForce).Within(Tolerance));
        }

        [Test]
        public void YawTorque_IsLimitedToMaxYawTorque()
        {
            var spinningWrongWay = new DroneState(new Vec3(0f, 5f, 0f), Vec3.Zero, 0f, -10f, isGrounded: false);

            var command = controller.Compute(new ControlInput(Vec3.Zero, 1f), spinningWrongWay);

            Assert.That(command.YawTorque, Is.EqualTo(drone.MaxYawTorque));
        }
    }
}
