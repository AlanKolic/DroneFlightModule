using System;
using NUnit.Framework;

namespace DroneFlightModule.Core.Tests
{
    public class FlightGateTests
    {
        private static readonly PilotId Pilot = new PilotId("pilot1");

        [Test]
        public void AllOf_RequiresAtLeastOneGate()
        {
            Assert.Throws<ArgumentNullException>(() => new AllOfFlightGate(null));
            Assert.Throws<ArgumentException>(() => new AllOfFlightGate(Array.Empty<IFlightGate>()));
            Assert.Throws<ArgumentException>(() => new AllOfFlightGate(new IFlightGate[] { null }));
        }

        [Test]
        public void AllOf_AllowsOnlyWhileEveryGateAllows()
        {
            var first = new ScriptedFlightGate();
            var second = new ScriptedFlightGate();
            var gate = new AllOfFlightGate(new IFlightGate[] { first, second });

            Assert.That(gate.IsFlightAllowed, Is.True);

            second.IsFlightAllowed = false;
            Assert.That(gate.IsFlightAllowed, Is.False);

            second.IsFlightAllowed = true;
            first.IsFlightAllowed = false;
            Assert.That(gate.IsFlightAllowed, Is.False);
        }

        [Test]
        public void AllOf_KeepsItsOwnCopyOfTheGates()
        {
            var closed = new ScriptedFlightGate { IsFlightAllowed = false };
            var gates = new IFlightGate[] { closed };
            var gate = new AllOfFlightGate(gates);

            gates[0] = new ScriptedFlightGate();

            Assert.That(gate.IsFlightAllowed, Is.False);
        }

        [Test]
        public void PilotInControl_RequiresRouter()
        {
            Assert.Throws<ArgumentNullException>(() => new PilotInControlGate(null));
        }

        [Test]
        public void PilotInControl_ClosedWithoutAssignedPilot()
        {
            var gate = new PilotInControlGate(new InputRouter());

            Assert.That(gate.IsFlightAllowed, Is.False);
        }

        [Test]
        public void PilotInControl_FollowsSourceConnection()
        {
            var router = new InputRouter();
            var source = new ScriptedInputProvider();
            var gate = new PilotInControlGate(router);

            router.Assign(Pilot, source);
            Assert.That(gate.IsFlightAllowed, Is.True);

            source.IsConnected = false;
            Assert.That(gate.IsFlightAllowed, Is.False);

            source.IsConnected = true;
            router.Clear();
            Assert.That(gate.IsFlightAllowed, Is.False);
        }
    }
}
