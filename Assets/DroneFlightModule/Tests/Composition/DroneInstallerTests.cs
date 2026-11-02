using System.IO;
using DroneFlightModule.Core;
using DroneFlightModule.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace DroneFlightModule.Composition.Tests
{
    public class DroneInstallerTests : InputTestFixture
    {
        private const string ControlsPath = "Assets/DroneFlightModule/Unity/Input/DroneControls.inputactions";
        private const float DeltaTime = 0.02f;

        private static readonly DroneId TestDrone = new DroneId("drone-1");

        private InputActionAsset controls;
        private IObjectResolver container;

        public override void Setup()
        {
            base.Setup();
            InputSystem.AddDevice<Keyboard>();
            controls = InputActionAsset.FromJson(File.ReadAllText(ControlsPath));
            var builder = new ContainerBuilder();
            new DroneInstaller(TestDrone, new Vec3(0f, 5f, 0f), groundHeight: 1f, controls).Install(builder);
            container = builder.Build();
        }

        public override void TearDown()
        {
            container.Dispose();
            Object.DestroyImmediate(controls);
            base.TearDown();
        }

        private void Run(FlightLoop loop, float seconds)
        {
            int ticks = Mathf.RoundToInt(seconds / DeltaTime);
            for (int i = 0; i < ticks; i++)
            {
                loop.Tick(DeltaTime);
            }
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
        public void AssignsLocalPilot()
        {
            var router = container.Resolve<InputRouter>();

            Assert.That(router.ActivePilotId, Is.EqualTo(DroneInstaller.LocalPilot));
            Assert.That(router.IsConnected, Is.True);
        }

        [Test]
        public void GateFollowsTheLocalPilotsLink()
        {
            var gate = container.Resolve<IFlightGate>();
            var link = container.Resolve<DisconnectableInputProvider>();

            Assert.That(gate.IsFlightAllowed, Is.True);

            link.IsLinkCut = true;
            Assert.That(gate.IsFlightAllowed, Is.False);
        }

        [Test]
        public void PilotConnected_CentredSticks_DroneHovers()
        {
            var loop = container.Resolve<FlightLoop>();

            Run(loop, 3f);

            Assert.That(loop.Mode, Is.EqualTo(FlightMode.Piloted));
            Assert.That(loop.State.Position.Y, Is.EqualTo(5f).Within(0.01f));
        }

        [Test]
        public void LinkCut_DroneHoldsThenLandsOnGroundHeight()
        {
            var loop = container.Resolve<FlightLoop>();
            container.Resolve<DisconnectableInputProvider>().IsLinkCut = true;

            Run(loop, 5f);
            Assert.That(loop.Mode, Is.EqualTo(FlightMode.Holding));

            Run(loop, 25f);
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

        [Test]
        public void DisposingTheContainer_DisablesTheControls()
        {
            container.Resolve<FlightLoop>();
            container.Resolve<LinkLossToggle>();
            Assert.That(controls.FindActionMap("Flight").enabled, Is.True);

            container.Dispose();

            Assert.That(controls.FindActionMap("Flight").enabled, Is.False);
            Assert.That(controls.FindAction("Debug/ToggleLink").enabled, Is.False);
        }
    }
}
