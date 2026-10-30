using System;
using NUnit.Framework;

namespace DroneFlightModule.Core.Tests
{
    public class DroneIntegratorTests
    {
        private const float Dt = TestConfigs.Dt;

        [Test]
        public void FreeFall_GainsSpeedAtGravity()
        {
            var integrator = new DroneIntegrator(TestConfigs.Dragless, new FlatTerrain());
            var state = DroneState.At(new Vec3(0f, 100f, 0f));

            for (int i = 0; i < 50; i++)
            {
                state = integrator.Step(state, ForceCommand.None, Dt);
            }

            Assert.That(state.Velocity.Y, Is.EqualTo(-DroneIntegrator.StandardGravity).Within(1e-3f));
            Assert.That(state.Position.Y, Is.LessThan(100f));
            Assert.That(state.IsGrounded, Is.False);
        }

        [Test]
        public void RestingOnGround_WithoutThrust_StaysGrounded()
        {
            var integrator = new DroneIntegrator(DroneConfig.Default, new FlatTerrain());
            var state = DroneState.At(Vec3.Zero);

            for (int i = 0; i < 10; i++)
            {
                state = integrator.Step(state, ForceCommand.None, Dt);
            }

            Assert.That(state.IsGrounded, Is.True);
            Assert.That(state.Position, Is.EqualTo(Vec3.Zero));
            Assert.That(state.Velocity, Is.EqualTo(Vec3.Zero));
        }

        [Test]
        public void ThrustAboveWeight_LiftsOff()
        {
            var config = DroneConfig.Default;
            var integrator = new DroneIntegrator(config, new FlatTerrain());
            var grounded = integrator.Step(DroneState.At(Vec3.Zero), ForceCommand.None, Dt);
            var fullThrust = new ForceCommand(new Vec3(0f, config.MaxThrust, 0f), 0f);

            var state = integrator.Step(grounded, fullThrust, Dt);

            Assert.That(state.IsGrounded, Is.False);
            Assert.That(state.Position.Y, Is.GreaterThan(0f));
        }

        [Test]
        public void LateralForceWhileGrounded_DoesNotSlide()
        {
            var integrator = new DroneIntegrator(DroneConfig.Default, new FlatTerrain());
            var state = DroneState.At(Vec3.Zero);
            var sideways = new ForceCommand(new Vec3(5f, 0f, 5f), 0f);

            for (int i = 0; i < 10; i++)
            {
                state = integrator.Step(state, sideways, Dt);
            }

            Assert.That(state.Position, Is.EqualTo(Vec3.Zero));
            Assert.That(state.IsGrounded, Is.True);
        }

        [Test]
        public void Falling_LandsOnTerrainHeight()
        {
            var integrator = new DroneIntegrator(DroneConfig.Default, new FlatTerrain(height: 5f));
            var state = DroneState.At(new Vec3(0f, 8f, 0f));

            for (int i = 0; i < 200 && !state.IsGrounded; i++)
            {
                state = integrator.Step(state, ForceCommand.None, Dt);
            }

            Assert.That(state.IsGrounded, Is.True);
            Assert.That(state.Position.Y, Is.EqualTo(5f));
            Assert.That(state.Velocity, Is.EqualTo(Vec3.Zero));
        }

        [Test]
        public void SteadyForce_ReachesDragTerminalVelocity()
        {
            var config = DroneConfig.Default;
            var integrator = new DroneIntegrator(config, new FlatTerrain(), gravity: 0f);
            var state = DroneState.At(new Vec3(0f, 10f, 0f));
            var forward = new ForceCommand(new Vec3(0f, 0f, config.MaxLateralForce), 0f);

            for (int i = 0; i < 1000; i++)
            {
                state = integrator.Step(state, forward, Dt);
            }

            float terminal = config.MaxLateralForce / config.LinearDrag;
            Assert.That(state.Velocity.Z, Is.EqualTo(terminal).Within(1e-2f));
        }

        [Test]
        public void YawTorque_SpinsUpAndKeepsYawWrapped()
        {
            var config = DroneConfig.Default;
            var integrator = new DroneIntegrator(config, new FlatTerrain(), gravity: 0f);
            var state = DroneState.At(new Vec3(0f, 10f, 0f));
            var spin = new ForceCommand(Vec3.Zero, config.MaxYawTorque);

            for (int i = 0; i < 1000; i++)
            {
                state = integrator.Step(state, spin, Dt);
                Assert.That(state.Yaw, Is.GreaterThan(-MathF.PI).And.LessThanOrEqualTo(MathF.PI));
            }

            float terminalRate = config.MaxYawTorque / config.AngularDrag;
            Assert.That(state.YawRate, Is.EqualTo(terminalRate).Within(1e-2f));
        }

        [Test]
        public void Constructor_RejectsNegativeGravity()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new DroneIntegrator(DroneConfig.Default, new FlatTerrain(), gravity: -1f));
        }
    }
}
