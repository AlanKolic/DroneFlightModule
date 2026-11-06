using System;
using DroneFlightModule.Authorization;
using DroneFlightModule.Core;
using NUnit.Framework;

namespace DroneFlightModule.Coordination.Tests
{
    public class DroneRegistryTests
    {
        private FleetDrone Drone(string id) => new TestDrone(id, isAirborne: false).Fleet;

        [Test]
        public void KeepsTheGivenOrder_AndFindsById()
        {
            var first = Drone("drone-1");
            var second = Drone("drone-2");

            var registry = new DroneRegistry(new[] { first, second });

            Assert.That(registry.Drones, Is.EqualTo(new[] { first, second }));
            Assert.That(registry.TryGet(second.Id, out var found), Is.True);
            Assert.That(found, Is.SameAs(second));
            Assert.That(registry.TryGet(new DroneId("drone-9"), out _), Is.False);
            Assert.That(registry.TryGet(default, out _), Is.False);
        }

        [Test]
        public void RejectsAnInvalidFleet()
        {
            Assert.Throws<ArgumentNullException>(() => new DroneRegistry(null));
            Assert.Throws<ArgumentNullException>(() => new DroneRegistry(new FleetDrone[] { null }));
            Assert.Throws<ArgumentException>(() => new DroneRegistry(new[] { Drone("drone-1"), Drone("drone-1") }));
        }

        [Test]
        public void DronesCanJoinLater_InOrder()
        {
            var registry = new DroneRegistry(new FleetDrone[0]);
            var first = Drone("drone-1");
            var second = Drone("drone-2");

            registry.Add(first);
            registry.Add(second);

            Assert.That(registry.Drones, Is.EqualTo(new[] { first, second }));
            Assert.That(registry.TryGet(second.Id, out var found), Is.True);
            Assert.That(found, Is.SameAs(second));
        }

        [Test]
        public void Add_RejectsNullAndDuplicates()
        {
            var registry = new DroneRegistry(new[] { Drone("drone-1") });

            Assert.Throws<ArgumentNullException>(() => registry.Add(null));
            Assert.Throws<ArgumentException>(() => registry.Add(Drone("drone-1")));
            Assert.That(registry.Drones, Has.Count.EqualTo(1));
        }

        [Test]
        public void FleetDrone_RequiresEveryPart()
        {
            var router = new InputRouter();
            var holder = new DroneGrantHolder(router);
            var arming = new ArmingSwitch();
            var status = Drone("drone-2").Status;

            Assert.Throws<ArgumentException>(() => new FleetDrone(default, router, holder, arming, status));
            Assert.Throws<ArgumentNullException>(() => new FleetDrone(new DroneId("drone-1"), null, holder, arming, status));
            Assert.Throws<ArgumentNullException>(() => new FleetDrone(new DroneId("drone-1"), router, null, arming, status));
            Assert.Throws<ArgumentNullException>(() => new FleetDrone(new DroneId("drone-1"), router, holder, null, status));
            Assert.Throws<ArgumentNullException>(() => new FleetDrone(new DroneId("drone-1"), router, holder, arming, null));
        }

        [Test]
        public void FlightLoopStatus_FollowsTheLoop()
        {
            var airborne = new TestDrone("drone-1", isAirborne: true);
            var grounded = new TestDrone("drone-2", isAirborne: false);

            Assert.That(airborne.Fleet.Status.IsGrounded, Is.False);
            Assert.That(grounded.Fleet.Status.IsGrounded, Is.True);
            Assert.Throws<ArgumentNullException>(() => new FlightLoopStatus(null));
        }
    }
}
