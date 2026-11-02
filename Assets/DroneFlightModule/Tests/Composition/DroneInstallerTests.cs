using DroneFlightModule.Core;
using NUnit.Framework;
using UnityEngine;
using VContainer;

namespace DroneFlightModule.Composition.Tests
{
    public class DroneInstallerTests
    {
        private static readonly DroneId TestDrone = new DroneId("drone-1");

        private IObjectResolver container;

        [SetUp]
        public void SetUp()
        {
            var builder = new ContainerBuilder();
            new DroneInstaller(TestDrone, new Vec3(0f, 5f, 0f), groundHeight: 1f).Install(builder);
            container = builder.Build();
        }

        [TearDown]
        public void TearDown()
        {
            container.Dispose();
        }

        [Test]
        public void ResolvesOneFlightLoopForTheDrone()
        {
            var loop = container.Resolve<FlightLoop>();

            Assert.That(loop.DroneId, Is.EqualTo(TestDrone));
            Assert.That(loop.State.Position, Is.EqualTo(new Vec3(0f, 5f, 0f)));
            Assert.That(container.Resolve<FlightLoop>(), Is.SameAs(loop));
        }

        [Test]
        public void AssignsLocalKeyboardPilot()
        {
            var router = container.Resolve<InputRouter>();

            Assert.That(router.ActivePilotId, Is.EqualTo(DroneInstaller.LocalKeyboardPilot));
        }

        [Test]
        public void WithoutInput_DroneFallsAndLandsOnGroundHeight()
        {
            var loop = container.Resolve<FlightLoop>();

            for (int i = 0; i < 200; i++)
            {
                loop.Tick(0.02f);
            }

            Assert.That(loop.State.IsGrounded, Is.True);
            Assert.That(loop.State.Position.Y, Is.EqualTo(1f));
        }

        [Test]
        public void Runner_TicksOneFixedStep()
        {
            var loop = container.Resolve<FlightLoop>();
            var runner = new FlightLoopRunner(loop);

            runner.FixedTick();

            Assert.That(loop.Time, Is.EqualTo(Time.fixedDeltaTime).Within(1e-6));
        }
    }
}
