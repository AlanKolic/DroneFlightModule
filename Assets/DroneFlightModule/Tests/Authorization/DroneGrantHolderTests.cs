using System;
using DroneFlightModule.Core;
using NUnit.Framework;

namespace DroneFlightModule.Authorization.Tests
{
    public class DroneGrantHolderTests
    {
        private static readonly PilotId Alice = new PilotId("alice");
        private static readonly PilotId Bob = new PilotId("bob");
        private static readonly DroneId Drone1 = new DroneId("drone-1");
        private static readonly DroneId Drone2 = new DroneId("drone-2");

        private GrantLedger ledger;
        private PilotClaims claims;
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
            claims = new PilotClaims();
            router = new InputRouter();
            holder = new DroneGrantHolder(Drone1, router, claims);
        }

        [Test]
        public void Constructor_RejectsInvalidArguments()
        {
            Assert.Throws<ArgumentException>(() => new DroneGrantHolder(default, router, claims));
            Assert.Throws<ArgumentNullException>(() => new DroneGrantHolder(Drone1, null, claims));
            Assert.Throws<ArgumentNullException>(() => new DroneGrantHolder(Drone1, router, null));
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
        public void Install_ClaimsTheDroneForThePilot()
        {
            holder.Install(ledger.Issue(Alice));

            Assert.That(claims.TryGetDrone(Alice, out var drone), Is.True);
            Assert.That(drone, Is.EqualTo(Drone1));
        }

        [Test]
        public void PilotControllingAnotherDrone_IsRefused()
        {
            var other = new DroneGrantHolder(Drone2, new InputRouter(), claims);
            var grant = ledger.Issue(Alice);
            other.Install(grant);

            Assert.Throws<InvalidOperationException>(() => holder.Install(grant));
            Assert.That(holder.Grant, Is.Null);
            Assert.That(other.Grant, Is.SameAs(grant));
        }

        [Test]
        public void Reinstalling_OnTheSameDrone_IsAllowed()
        {
            holder.Install(ledger.Issue(Alice));

            Assert.DoesNotThrow(() => holder.Install(ledger.Issue(Alice)));
        }

        [Test]
        public void Clear_ReleasesTheClaim()
        {
            var other = new DroneGrantHolder(Drone2, new InputRouter(), claims);
            other.Install(ledger.Issue(Alice));

            other.Clear();
            holder.Install(ledger.Issue(Alice));

            Assert.That(claims.TryGetDrone(Alice, out var drone), Is.True);
            Assert.That(drone, Is.EqualTo(Drone1));
        }

        [Test]
        public void ReplacingTheGrant_ReleasesThePreviousPilot()
        {
            holder.Install(ledger.Issue(Alice));

            holder.Install(ledger.Issue(Bob));

            Assert.That(claims.TryGetDrone(Alice, out _), Is.False);
            Assert.That(claims.TryGetDrone(Bob, out _), Is.True);
        }

        [Test]
        public void Revocation_ReleasesTheClaim()
        {
            holder.Install(ledger.Issue(Alice));

            ledger.Revoke(Alice);

            Assert.That(claims.TryGetDrone(Alice, out _), Is.False);
        }

        [Test]
        public void RevokingAReplacedGrant_KeepsTheCurrentClaim()
        {
            var old = new GrantLedger("old-provider");
            holder.Install(old.Issue(Alice));
            holder.Install(ledger.Issue(Alice));

            old.Revoke(Alice);

            Assert.That(claims.TryGetDrone(Alice, out _), Is.True);
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
