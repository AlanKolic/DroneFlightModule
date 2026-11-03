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
        private List<PilotId> changes;

        [SetUp]
        public void SetUp()
        {
            router = new InputRouter();
            changes = new List<PilotId>();
            router.ActiveChanged += changes.Add;
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
            var phone = new ScriptedInputProvider { Current = Climb };
            router.Assign(Pilot1, phone);

            phone.IsConnected = false;

            Assert.That(router.Read(), Is.EqualTo(ControlInput.Zero));
            Assert.That(router.IsConnected, Is.False);
            Assert.That(router.ActivePilotId, Is.EqualTo(Pilot1));
        }

        [Test]
        public void ReconnectedSource_RoutesAgain()
        {
            var phone = new ScriptedInputProvider { Current = Climb, IsConnected = false };
            router.Assign(Pilot1, phone);

            phone.IsConnected = true;

            Assert.That(router.Read(), Is.EqualTo(Climb));
        }

        [Test]
        public void AssigningAnotherPilot_SwitchesInputAndRaisesChange()
        {
            router.Assign(Pilot1, new ScriptedInputProvider { Current = Climb });

            router.Assign(Pilot2, new ScriptedInputProvider { Current = Forward });

            Assert.That(router.Read(), Is.EqualTo(Forward));
            Assert.That(router.ActivePilotId, Is.EqualTo(Pilot2));
            Assert.That(changes, Is.EqualTo(new[] { Pilot1, Pilot2 }));
        }

        [Test]
        public void SamePilotWithNewSource_SwitchesInputAndRaisesOnlySourceChange()
        {
            var sourceChanges = new List<PilotId>();
            router.SourceChanged += sourceChanges.Add;
            router.Assign(Pilot1, new ScriptedInputProvider { Current = Climb });

            router.Assign(Pilot1, new ScriptedInputProvider { Current = Forward });

            Assert.That(router.Read(), Is.EqualTo(Forward));
            Assert.That(changes, Is.EqualTo(new[] { Pilot1 }));
            Assert.That(sourceChanges, Is.EqualTo(new[] { Pilot1, Pilot1 }));
        }

        [Test]
        public void SameSourceAgain_RaisesNoSourceChange_ClearRaisesOne()
        {
            var sourceChanges = new List<PilotId>();
            var keyboard = new ScriptedInputProvider { Current = Climb };
            router.Assign(Pilot1, keyboard);
            router.SourceChanged += sourceChanges.Add;

            router.Assign(Pilot1, keyboard);
            Assert.That(sourceChanges, Is.Empty);

            router.Clear();
            router.Clear();
            Assert.That(sourceChanges, Has.Count.EqualTo(1));
            Assert.That(sourceChanges[0].IsEmpty, Is.True);
        }

        [Test]
        public void Clear_StopsRoutingAndRaisesEmptyPilot()
        {
            router.Assign(Pilot1, new ScriptedInputProvider { Current = Climb });

            router.Clear();

            Assert.That(router.Read(), Is.EqualTo(ControlInput.Zero));
            Assert.That(router.IsConnected, Is.False);
            Assert.That(router.ActivePilotId.IsEmpty, Is.True);
            Assert.That(changes, Has.Count.EqualTo(2));
            Assert.That(changes[1].IsEmpty, Is.True);
        }

        [Test]
        public void ClearWhenEmpty_RaisesNothing()
        {
            router.Clear();

            Assert.That(changes, Is.Empty);
        }

        [Test]
        public void Assign_RejectsMissingPilotOrSource()
        {
            Assert.Throws<ArgumentException>(() => router.Assign(default, new ScriptedInputProvider()));
            Assert.Throws<ArgumentNullException>(() => router.Assign(Pilot1, null));
            Assert.Throws<ArgumentException>(() => router.Assign(Pilot1, router));
        }
    }
}
