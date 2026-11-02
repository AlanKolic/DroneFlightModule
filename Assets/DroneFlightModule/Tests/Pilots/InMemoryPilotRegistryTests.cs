using System;
using DroneFlightModule.Core;
using NUnit.Framework;

namespace DroneFlightModule.Pilots.Tests
{
    public class InMemoryPilotRegistryTests
    {
        private static readonly PilotProfile Alice = new PilotProfile(new PilotId("alice"), "Alice", 5);
        private static readonly PilotProfile Bob = new PilotProfile(new PilotId("bob"), "Bob", 10);
        private static readonly PilotProfile Carol = new PilotProfile(new PilotId("carol"), "Carol", 5);

        [Test]
        public void Constructor_RequiresRoster()
        {
            Assert.Throws<ArgumentNullException>(() => new InMemoryPilotRegistry(null));
        }

        [Test]
        public void EmptyRoster_StillKnowsGuest()
        {
            var registry = new InMemoryPilotRegistry(Array.Empty<PilotProfile>());

            Assert.That(registry.TryGet(PilotProfile.Guest.Id, out var guest), Is.True);
            Assert.That(guest, Is.SameAs(PilotProfile.Guest));
            Assert.That(registry.Profiles, Is.EqualTo(new[] { PilotProfile.Guest }));
        }

        [Test]
        public void KnownPilot_IsFound()
        {
            var registry = new InMemoryPilotRegistry(new[] { Alice, Bob });

            Assert.That(registry.TryGet(new PilotId("bob"), out var profile), Is.True);
            Assert.That(profile, Is.SameAs(Bob));
        }

        [Test]
        public void UnknownOrEmptyPilot_IsNotFound()
        {
            var registry = new InMemoryPilotRegistry(new[] { Alice });

            Assert.That(registry.TryGet(new PilotId("mallory"), out var unknown), Is.False);
            Assert.That(unknown, Is.Null);
            Assert.That(registry.TryGet(default, out var empty), Is.False);
            Assert.That(empty, Is.Null);
        }

        [Test]
        public void Profiles_AreHighestPriorityFirst_TiesKeepRosterOrder_GuestLast()
        {
            var registry = new InMemoryPilotRegistry(new[] { Alice, Bob, Carol });

            Assert.That(registry.Profiles, Is.EqualTo(new[] { Bob, Alice, Carol, PilotProfile.Guest }));
        }

        [Test]
        public void RejectsDuplicates()
        {
            var aliceAgain = new PilotProfile(new PilotId("alice"), "Alice Again", 7);

            Assert.Throws<ArgumentException>(() => new InMemoryPilotRegistry(new[] { Alice, aliceAgain }));
        }

        [Test]
        public void RejectsNullProfiles()
        {
            Assert.Throws<ArgumentException>(() => new InMemoryPilotRegistry(new[] { Alice, null }));
        }

        [Test]
        public void RejectsRegisteringTheGuest()
        {
            var fakeGuest = new PilotProfile(PilotProfile.Guest.Id, "Not Really A Guest", 50);

            Assert.Throws<ArgumentException>(() => new InMemoryPilotRegistry(new[] { PilotProfile.Guest }));
            Assert.Throws<ArgumentException>(() => new InMemoryPilotRegistry(new[] { fakeGuest }));
        }

        [Test]
        public void RejectsPilotsThatDoNotOutrankTheGuest()
        {
            var equalToGuest = new PilotProfile(new PilotId("dave"), "Dave", PilotProfile.GuestPriority);

            Assert.Throws<ArgumentException>(() => new InMemoryPilotRegistry(new[] { equalToGuest }));
        }
    }
}
