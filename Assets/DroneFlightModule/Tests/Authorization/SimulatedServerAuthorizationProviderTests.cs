using System;
using System.Threading;
using System.Threading.Tasks;
using DroneFlightModule.Core;
using NUnit.Framework;

namespace DroneFlightModule.Authorization.Tests
{
    public class SimulatedServerAuthorizationProviderTests : SynchronousAsyncFixture
    {
        private static readonly PilotId Alice = new PilotId("alice");
        private static readonly PilotId Bob = new PilotId("bob");
        private static readonly TimeSpan Latency = TimeSpan.FromMilliseconds(400);

        private ManualDelay delay;
        private SimulatedServerAuthorizationProvider server;

        [SetUp]
        public void SetUp()
        {
            delay = new ManualDelay();
            server = new SimulatedServerAuthorizationProvider(new[] { Alice }, Latency, delay);
        }

        [TearDown]
        public void TearDown()
        {
            server.Dispose();
        }

        private Task<AuthorizationResult> Request(PilotId pilot, PilotOrigin origin = PilotOrigin.Remote, CancellationToken cancellationToken = default)
        {
            return server.AuthorizeAsync(new AuthorizationRequest(pilot, origin), cancellationToken);
        }

        [Test]
        public void Constructor_RejectsInvalidArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new SimulatedServerAuthorizationProvider(null, Latency, delay));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SimulatedServerAuthorizationProvider(new[] { Alice }, TimeSpan.FromSeconds(-1), delay));
            Assert.Throws<ArgumentNullException>(() => new SimulatedServerAuthorizationProvider(new[] { Alice }, Latency, null));
        }

        [Test]
        public void Answer_ArrivesOnlyAfterTheLatency()
        {
            var task = Request(Alice);

            Assert.That(task.IsCompleted, Is.False);
            Assert.That(delay.LastDuration, Is.EqualTo(Latency));

            delay.CompleteAll();

            Assert.That(task.IsCompleted, Is.True);
            Assert.That(task.Result.IsGranted, Is.True);
            Assert.That(task.Result.Grant.ProviderId, Is.EqualTo(SimulatedServerAuthorizationProvider.ProviderId));
        }

        [Test]
        public void ApprovedPilot_IsGrantedFromEitherOrigin()
        {
            var remote = Request(Alice, PilotOrigin.Remote);
            var local = Request(Alice, PilotOrigin.Local);
            delay.CompleteAll();

            Assert.That(remote.Result.IsGranted, Is.True);
            Assert.That(local.Result.IsGranted, Is.True);
            Assert.That(local.Result.Grant, Is.SameAs(remote.Result.Grant));
        }

        [Test]
        public void UnapprovedPilot_IsDenied()
        {
            var task = Request(Bob);
            delay.CompleteAll();

            Assert.That(task.Result.IsGranted, Is.False);
            StringAssert.Contains("bob", task.Result.DenialReason);
        }

        [Test]
        public void PolicyIsReadWhenTheAnswerArrives()
        {
            var bob = Request(Bob);
            var alice = Request(Alice);

            server.Approve(Bob);
            server.Deny(Alice);
            delay.CompleteAll();

            Assert.That(bob.Result.IsGranted, Is.True);
            Assert.That(alice.Result.IsGranted, Is.False);
        }

        [Test]
        public void Revoke_CancelsThePilotsGrant()
        {
            var task = Request(Alice);
            delay.CompleteAll();
            var grant = task.Result.Grant;

            Assert.That(server.Revoke(Alice), Is.True);

            Assert.That(grant.IsRevoked, Is.True);
            Assert.That(server.Revoke(Alice), Is.False);
        }

        [Test]
        public void CancellationMidRequest_CancelsWithoutIssuingAGrant()
        {
            using (var cancellation = new CancellationTokenSource())
            {
                var task = Request(Alice, cancellationToken: cancellation.Token);

                cancellation.Cancel();

                Assert.That(task.IsCanceled, Is.True);
                Assert.That(server.Revoke(Alice), Is.False, "No grant should have been issued.");
            }
        }

        [Test]
        public void Dispose_CancelsPendingRequestsAndRevokesGrants()
        {
            var granted = Request(Alice);
            delay.CompleteAll();
            var grant = granted.Result.Grant;
            var pending = Request(Alice);

            server.Dispose();

            Assert.That(pending.IsCanceled, Is.True);
            Assert.That(grant.IsRevoked, Is.True);
        }

        [Test]
        public void AfterDispose_RequestsFail()
        {
            server.Dispose();

            var task = Request(Alice);

            Assert.That(task.IsFaulted, Is.True);
            Assert.That(task.Exception.InnerException, Is.TypeOf<ObjectDisposedException>());
        }

        [Test]
        public void NullRequest_ThrowsSynchronously()
        {
            Assert.Throws<ArgumentNullException>(() => server.AuthorizeAsync(null, CancellationToken.None));
        }

        [Test]
        public void DisposedWhileTheAnswerIsInFlight_IssuesNoGrant()
        {
            var ignoresCancellation = new CancellationIgnoringDelay();
            using (var racing = new SimulatedServerAuthorizationProvider(new[] { Alice }, Latency, ignoresCancellation))
            {
                var task = racing.AuthorizeAsync(new AuthorizationRequest(Alice, PilotOrigin.Remote), CancellationToken.None);

                racing.Dispose();
                ignoresCancellation.Complete();

                Assert.That(task.IsCanceled, Is.True);
                Assert.That(racing.Revoke(Alice), Is.False, "No grant should have been issued.");
            }
        }

        [Test]
        public void ZeroLatency_WithRealDelay_AnswersImmediately()
        {
            using (var instant = new SimulatedServerAuthorizationProvider(new[] { Alice }, TimeSpan.Zero, new TaskDelay()))
            {
                var task = instant.AuthorizeAsync(new AuthorizationRequest(Alice, PilotOrigin.Remote), CancellationToken.None);

                Assert.That(task.IsCompleted, Is.True);
                Assert.That(task.Result.IsGranted, Is.True);
            }
        }
    }
}
