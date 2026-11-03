using System;
using System.Threading;
using DroneFlightModule.Core;
using NUnit.Framework;

namespace DroneFlightModule.Authorization.Tests
{
    public class LocalGuestAuthorizationProviderTests
    {
        private static readonly PilotId Guest = new PilotId("guest");
        private static readonly PilotId Alice = new PilotId("alice");

        private LocalGuestAuthorizationProvider provider;

        [SetUp]
        public void SetUp()
        {
            provider = new LocalGuestAuthorizationProvider(Guest);
        }

        [TearDown]
        public void TearDown()
        {
            provider.Dispose();
        }

        private AuthorizationResult Authorize(PilotId pilot, PilotOrigin origin)
        {
            var task = provider.AuthorizeAsync(new AuthorizationRequest(pilot, origin), CancellationToken.None);
            Assert.That(task.IsCompleted, Is.True, "The guest provider answers immediately.");
            return task.Result;
        }

        [Test]
        public void Constructor_RequiresGuestPilot()
        {
            Assert.Throws<ArgumentException>(() => new LocalGuestAuthorizationProvider(default));
        }

        [Test]
        public void LocalGuest_IsGrantedImmediately()
        {
            var result = Authorize(Guest, PilotOrigin.Local);

            Assert.That(result.IsGranted, Is.True);
            Assert.That(result.Grant.PilotId, Is.EqualTo(Guest));
            Assert.That(result.Grant.ProviderId, Is.EqualTo(LocalGuestAuthorizationProvider.ProviderId));
        }

        [Test]
        public void RemoteRequest_IsDenied()
        {
            var result = Authorize(Guest, PilotOrigin.Remote);

            Assert.That(result.IsGranted, Is.False);
            StringAssert.Contains("local machine", result.DenialReason);
        }

        [Test]
        public void LocalRegisteredPilot_IsDenied()
        {
            var result = Authorize(Alice, PilotOrigin.Local);

            Assert.That(result.IsGranted, Is.False);
        }

        [Test]
        public void RepeatedRequests_ShareOneGrant()
        {
            Assert.That(Authorize(Guest, PilotOrigin.Local).Grant, Is.SameAs(Authorize(Guest, PilotOrigin.Local).Grant));
        }

        [Test]
        public void CancelledToken_CancelsTheRequest()
        {
            var task = provider.AuthorizeAsync(new AuthorizationRequest(Guest, PilotOrigin.Local), new CancellationToken(true));

            Assert.That(task.IsCanceled, Is.True);
        }

        [Test]
        public void NullRequest_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => provider.AuthorizeAsync(null, CancellationToken.None));
        }

        [Test]
        public void Dispose_RevokesGrantsAndRejectsLaterRequests()
        {
            var grant = Authorize(Guest, PilotOrigin.Local).Grant;

            provider.Dispose();

            Assert.That(grant.IsRevoked, Is.True);
            var task = provider.AuthorizeAsync(new AuthorizationRequest(Guest, PilotOrigin.Local), CancellationToken.None);
            Assert.That(task.IsFaulted, Is.True);
            Assert.That(task.Exception.InnerException, Is.TypeOf<ObjectDisposedException>());
        }
    }
}
