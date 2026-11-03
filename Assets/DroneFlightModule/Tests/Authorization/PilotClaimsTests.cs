using System;
using DroneFlightModule.Core;
using NUnit.Framework;

namespace DroneFlightModule.Authorization.Tests
{
    public class PilotClaimsTests
    {
        private static readonly PilotId Alice = new PilotId("alice");
        private static readonly PilotId Bob = new PilotId("bob");
        private static readonly DroneId Drone1 = new DroneId("drone-1");
        private static readonly DroneId Drone2 = new DroneId("drone-2");

        private PilotClaims claims;

        [SetUp]
        public void SetUp()
        {
            claims = new PilotClaims();
        }

        [Test]
        public void Claim_RecordsTheDrone()
        {
            claims.Claim(Alice, Drone1);

            Assert.That(claims.TryGetDrone(Alice, out var drone), Is.True);
            Assert.That(drone, Is.EqualTo(Drone1));
            Assert.That(claims.TryGetDrone(Bob, out _), Is.False);
        }

        [Test]
        public void ClaimingASecondDrone_Throws()
        {
            claims.Claim(Alice, Drone1);

            Assert.DoesNotThrow(() => claims.Claim(Alice, Drone1));
            Assert.Throws<InvalidOperationException>(() => claims.Claim(Alice, Drone2));
        }

        [Test]
        public void Release_OnlyForTheClaimedDrone()
        {
            claims.Claim(Alice, Drone1);

            Assert.That(claims.Release(Alice, Drone2), Is.False);
            Assert.That(claims.Release(Alice, Drone1), Is.True);
            Assert.That(claims.Release(Alice, Drone1), Is.False);
            Assert.DoesNotThrow(() => claims.Claim(Alice, Drone2));
        }

        [Test]
        public void EmptyIds_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => claims.Claim(default, Drone1));
            Assert.Throws<ArgumentException>(() => claims.Claim(Alice, default));
        }
    }
}
