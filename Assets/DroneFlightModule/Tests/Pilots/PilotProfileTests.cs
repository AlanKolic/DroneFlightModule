using System;
using DroneFlightModule.Core;
using NUnit.Framework;

namespace DroneFlightModule.Pilots.Tests
{
    public class PilotProfileTests
    {
        private static readonly PilotId Alice = new PilotId("alice");

        [Test]
        public void Constructor_RejectsMissingIdOrName()
        {
            Assert.Throws<ArgumentException>(() => new PilotProfile(default, "Alice", 5));
            Assert.Throws<ArgumentException>(() => new PilotProfile(Alice, " ", 5));
            Assert.Throws<ArgumentException>(() => new PilotProfile(Alice, null, 5));
        }

        [Test]
        public void Constructor_RejectsPriorityBelowGuest()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PilotProfile(Alice, "Alice", -1));
        }

        [Test]
        public void Guest_HasLowestPriorityAndIsGuest()
        {
            Assert.That(PilotProfile.Guest.Priority, Is.EqualTo(PilotProfile.GuestPriority));
            Assert.That(PilotProfile.Guest.IsGuest, Is.True);
            Assert.That(new PilotProfile(Alice, "Alice", 5).IsGuest, Is.False);
        }
    }
}
