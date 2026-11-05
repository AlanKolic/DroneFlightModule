using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DroneFlightModule.Core.Tests
{
    public class InputRouterTests
    {
        private static readonly PilotId Pilot1 = new PilotId("pilot1");
        private static readonly PilotId Pilot2 = new PilotId("pilot2");
        private static readonly ControlInput Climb = new ControlInput(Vec3.Up, 0f);
        private static readonly ControlInput Forward = new ControlInput(Vec3.Forward, 0f);

        private InputRouter router;

        [SetUp]
        public void SetUp()
        {
            router = new InputRouter();
        }

        [Test]
        public void NothingAssigned_ReadsZeroAndReportsNoPilot()
        {
            Assert.That(router.Read(), Is.EqualTo(ControlInput.Zero));
            Assert.That(router.IsConnected, Is.False);
            Assert.That(router.ActivePilotId.IsEmpty, Is.True);
        }

        [Test]
        public void Assigned_RoutesSourceInput()
        {
            router.Assign(Pilot1, new ScriptedInputProvider { Current = Climb });

            Assert.That(router.Read(), Is.EqualTo(Climb));
            Assert.That(router.IsConnected, Is.True);
            Assert.That(router.ActivePilotId, Is.EqualTo(Pilot1));
        }

        [Test]
        public void DisconnectedSource_ReadsZeroButKeepsPilot()
        {
            var pad = new ScriptedInputProvider { Current = Climb };
            router.Assign(Pilot1, pad);

            pad.IsConnected = false;

            Assert.That(router.Read(), Is.EqualTo(ControlInput.Zero));
            Assert.That(router.IsConnected, Is.False);
            Assert.That(router.ActivePilotId, Is.EqualTo(Pilot1));
        }

        [Test]
        public void ReconnectedSource_RoutesAgain()
        {
            var pad = new ScriptedInputProvider { Current = Climb, IsConnected = false };
            router.Assign(Pilot1, pad);

            pad.IsConnected = true;

            Assert.That(router.Read(), Is.EqualTo(Climb));
        }

        [Test]
        public void AssigningAnotherPilot_SwitchesInput()
        {
            router.Assign(Pilot1, new ScriptedInputProvider { Current = Climb });

            router.Assign(Pilot2, new ScriptedInputProvider { Current = Forward });

            Assert.That(router.Read(), Is.EqualTo(Forward));
            Assert.That(router.ActivePilotId, Is.EqualTo(Pilot2));
        }

        [Test]
        public void SamePilotWithNewSource_SwitchesInput()
        {
            router.Assign(Pilot1, new ScriptedInputProvider { Current = Climb });

            router.Assign(Pilot1, new ScriptedInputProvider { Current = Forward });

            Assert.That(router.Read(), Is.EqualTo(Forward));
            Assert.That(router.ActivePilotId, Is.EqualTo(Pilot1));
        }

        [Test]
        public void Clear_StopsRouting()
        {
            router.Assign(Pilot1, new ScriptedInputProvider { Current = Climb });

            router.Clear();

            Assert.That(router.Read(), Is.EqualTo(ControlInput.Zero));
            Assert.That(router.IsConnected, Is.False);
            Assert.That(router.ActivePilotId.IsEmpty, Is.True);
        }

        [Test]
        public void Assign_RejectsMissingPilotOrSource()
        {
            Assert.Throws<ArgumentException>(() => router.Assign(default, new ScriptedInputProvider()));
            Assert.Throws<ArgumentNullException>(() => router.Assign(Pilot1, null));
        }
    }
}
