using System;
using System.Threading;
using DroneFlightModule.Core;
using NUnit.Framework;

namespace DroneFlightModule.Authorization.Tests
{
    public class GrantTests
    {
        private static readonly PilotId Alice = new PilotId("alice");
        private static readonly PilotId Bob = new PilotId("bob");

        private GrantLedger ledger;

        [SetUp]
        public void SetUp()
        {
            ledger = new GrantLedger("test-provider");
        }

        [Test]
        public void Request_And_Grant_RequireIds()
        {
            Assert.Throws<ArgumentException>(() => new AuthorizationRequest(default));
            Assert.Throws<ArgumentException>(() => new AuthorizationGrant(default, "provider", CancellationToken.None));
            Assert.Throws<ArgumentException>(() => new AuthorizationGrant(Alice, " ", CancellationToken.None));
            Assert.Throws<ArgumentException>(() => new GrantLedger(null));
        }

        [Test]
        public void Result_GrantedOrDeniedWithReason()
        {
            var grant = ledger.Issue(Alice);

            Assert.That(new AuthorizationResult(grant).IsGranted, Is.True);
            Assert.That(new AuthorizationResult("no").IsGranted, Is.False);
            Assert.That(new AuthorizationResult("no").DenialReason, Is.EqualTo("no"));
            Assert.Throws<ArgumentNullException>(() => new AuthorizationResult((AuthorizationGrant)null));
            Assert.Throws<ArgumentException>(() => new AuthorizationResult(""));
        }

        [Test]
        public void Issue_GivesALiveGrantForThePilot()
        {
            var grant = ledger.Issue(Alice);

            Assert.That(grant.PilotId, Is.EqualTo(Alice));
            Assert.That(grant.ProviderId, Is.EqualTo("test-provider"));
            Assert.That(grant.IsRevoked, Is.False);
        }

        [Test]
        public void Issue_Again_ReturnsTheSameLiveGrant()
        {
            Assert.That(ledger.Issue(Alice), Is.SameAs(ledger.Issue(Alice)));
            Assert.That(ledger.Issue(Alice), Is.Not.SameAs(ledger.Issue(Bob)));
        }

        [Test]
        public void Revoke_CancelsTheGrantAndRunsCallbacks()
        {
            var grant = ledger.Issue(Alice);
            bool notified = false;
            grant.Revoked.Register(() => notified = true);

            Assert.That(ledger.Revoke(Alice), Is.True);

            Assert.That(grant.IsRevoked, Is.True);
            Assert.That(notified, Is.True);
        }

        [Test]
        public void Revoke_UnknownPilot_ReturnsFalse()
        {
            Assert.That(ledger.Revoke(Alice), Is.False);
        }

        [Test]
        public void Issue_AfterRevoke_GivesAFreshGrant()
        {
            var first = ledger.Issue(Alice);
            ledger.Revoke(Alice);

            var second = ledger.Issue(Alice);

            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.IsRevoked, Is.False);
        }

        [Test]
        public void RevokedToken_StillAcceptsRegistrations()
        {
            var grant = ledger.Issue(Alice);
            ledger.Revoke(Alice);
            bool notified = false;

            grant.Revoked.Register(() => notified = true);

            Assert.That(notified, Is.True);
        }

        [Test]
        public void Dispose_RevokesEveryGrant()
        {
            var alice = ledger.Issue(Alice);
            var bob = ledger.Issue(Bob);

            ledger.Dispose();

            Assert.That(alice.IsRevoked, Is.True);
            Assert.That(bob.IsRevoked, Is.True);
        }
    }
}
