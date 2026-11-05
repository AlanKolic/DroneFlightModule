using System;
using DroneFlightModule.Core;
using NUnit.Framework;

namespace DroneFlightModule.Authorization.Tests
{
    public class DroneGrantHolderTests
    {
        private static readonly PilotId Alice = new PilotId("alice");
        private static readonly PilotId Bob = new PilotId("bob");

        private GrantLedger ledger;
        private InputRouter router;
        private DroneGrantHolder holder;

        private sealed class ScriptedConnection : IInputProvider
        {
            public bool IsConnected { get; set; } = true;
            public ControlInput Read() => ControlInput.Zero;
        }

        [SetUp]
        public void SetUp()
        {
            ledger = new GrantLedger("test-provider");
            router = new InputRouter();
            holder = new DroneGrantHolder(router);
        }

        [Test]
        public void Constructor_RejectsInvalidArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new DroneGrantHolder(null));
        }

        [Test]
        public void NoGrant_IsClosed()
        {
            router.Assign(Alice, new ScriptedConnection());

            Assert.That(holder.IsFlightAllowed, Is.False);
        }

        [Test]
        public void GrantForTheActivePilot_IsOpen()
        {
            router.Assign(Alice, new ScriptedConnection());

            holder.Install(ledger.Issue(Alice));

            Assert.That(holder.IsFlightAllowed, Is.True);
        }

        [Test]
        public void GrantForAnotherPilot_IsClosed()
        {
            router.Assign(Bob, new ScriptedConnection());

            holder.Install(ledger.Issue(Alice));

            Assert.That(holder.IsFlightAllowed, Is.False);
        }

        [Test]
        public void RouterSwitchesPilot_ClosesUntilTheirGrantIsInstalled()
        {
            router.Assign(Alice, new ScriptedConnection());
            holder.Install(ledger.Issue(Alice));

            router.Assign(Bob, new ScriptedConnection());
            Assert.That(holder.IsFlightAllowed, Is.False);

            holder.Install(ledger.Issue(Bob));
            Assert.That(holder.IsFlightAllowed, Is.True);
        }

        [Test]
        public void Revocation_Closes()
        {
            router.Assign(Alice, new ScriptedConnection());
            holder.Install(ledger.Issue(Alice));

            ledger.Revoke(Alice);

            Assert.That(holder.IsFlightAllowed, Is.False);
        }

        [Test]
        public void Clear_Closes()
        {
            router.Assign(Alice, new ScriptedConnection());
            holder.Install(ledger.Issue(Alice));

            holder.Clear();

            Assert.That(holder.IsFlightAllowed, Is.False);
            Assert.That(holder.Grant, Is.Null);
        }

        [Test]
        public void Install_RequiresALiveGrant()
        {
            var revoked = ledger.Issue(Alice);
            ledger.Revoke(Alice);

            Assert.Throws<ArgumentNullException>(() => holder.Install(null));
            Assert.Throws<ArgumentException>(() => holder.Install(revoked));
        }

        [Test]
        public void ReplacingTheGrant_GatesOnTheNewPilot()
        {
            router.Assign(Bob, new ScriptedConnection());
            holder.Install(ledger.Issue(Alice));

            holder.Install(ledger.Issue(Bob));

            Assert.That(holder.Grant.PilotId, Is.EqualTo(Bob));
            Assert.That(holder.IsFlightAllowed, Is.True);
        }

        [Test]
        public void CombinedWithPilotInControl_NeedsBoth()
        {
            var source = new ScriptedConnection();
            router.Assign(Alice, source);
            var gate = new AllOfFlightGate(new IFlightGate[] { new PilotInControlGate(router), holder });
            holder.Install(ledger.Issue(Alice));
            Assert.That(gate.IsFlightAllowed, Is.True);

            source.IsConnected = false;
            Assert.That(gate.IsFlightAllowed, Is.False);

            source.IsConnected = true;
            ledger.Revoke(Alice);
            Assert.That(gate.IsFlightAllowed, Is.False);
        }
    }
}
