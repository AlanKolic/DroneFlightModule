using System;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;
using NUnit.Framework;

namespace DroneFlightModule.Coordination.Tests
{
    public class PriorityAssignmentPolicyTests
    {
        private static readonly DroneId Drone1 = new DroneId("drone-1");
        private static readonly DroneId Drone2 = new DroneId("drone-2");
        private static readonly PilotProfile Low = new PilotProfile(new PilotId("low"), "Low", 5);
        private static readonly PilotProfile Middle = new PilotProfile(new PilotId("middle"), "Middle", 10);
        private static readonly PilotProfile High = new PilotProfile(new PilotId("high"), "High", 20);

        private readonly PriorityAssignmentPolicy policy = new PriorityAssignmentPolicy();

        private DroneSnapshot Free(DroneId id, bool isGrounded = false) => new DroneSnapshot(id, isGrounded, null);

        private DroneSnapshot Flown(DroneId id, PilotProfile pilot) => new DroneSnapshot(id, false, pilot);

        private void AssertDecision(AssignmentDecision decision, AssignmentAction action, DroneId drone)
        {
            Assert.That(decision.Action, Is.EqualTo(action));
            Assert.That(decision.Drone, Is.EqualTo(drone));
        }

        [Test]
        public void Arguments_AreRequired()
        {
            Assert.Throws<ArgumentNullException>(() => policy.Decide(null, null, new DroneSnapshot[0]));
            Assert.Throws<ArgumentNullException>(() => policy.Decide(Middle, null, null));
        }

        [Test]
        public void PreferredFreeDrone_IsAssigned()
        {
            AssertDecision(policy.Decide(Middle, Drone2, new[] { Free(Drone1), Free(Drone2) }), AssignmentAction.Assign, Drone2);
        }

        [Test]
        public void PreferredDrone_FlownByALowerPilot_IsTakenOver()
        {
            AssertDecision(policy.Decide(Middle, Drone1, new[] { Flown(Drone1, Low), Free(Drone2) }), AssignmentAction.TakeOver, Drone1);
        }

        [Test]
        public void PreferredDrone_FlownByAnEqualOrHigherPilot_IsDenied()
        {
            var equal = new PilotProfile(new PilotId("equal"), "Equal", Middle.Priority);

            Assert.That(policy.Decide(Middle, Drone1, new[] { Flown(Drone1, equal) }).Denial, Is.EqualTo(FlightDenial.NoDroneAvailable));
            Assert.That(policy.Decide(Middle, Drone1, new[] { Flown(Drone1, High) }).Action, Is.EqualTo(AssignmentAction.Deny));
        }

        [Test]
        public void PreferredDrone_NotInTheFleet_IsUnknown()
        {
            Assert.That(policy.Decide(Middle, Drone2, new[] { Free(Drone1) }).Denial, Is.EqualTo(FlightDenial.UnknownDrone));
        }

        [Test]
        public void AnyDrone_PrefersAGroundedFreeOne()
        {
            AssertDecision(policy.Decide(Middle, null, new[] { Free(Drone1), Free(Drone2, isGrounded: true) }), AssignmentAction.Assign, Drone2);
        }

        [Test]
        public void AnyDrone_TakesTheFirstFreeOne_WhenNoneIsGrounded()
        {
            AssertDecision(policy.Decide(Middle, null, new[] { Flown(Drone1, Low), Free(Drone2) }), AssignmentAction.Assign, Drone2);
            AssertDecision(policy.Decide(Middle, null, new[] { Free(Drone1), Free(Drone2) }), AssignmentAction.Assign, Drone1);
        }

        [Test]
        public void AnyDrone_NoneFree_TakesOverTheLowestPilot()
        {
            AssertDecision(policy.Decide(High, null, new[] { Flown(Drone1, Middle), Flown(Drone2, Low) }), AssignmentAction.TakeOver, Drone2);
        }

        [Test]
        public void AnyDrone_NoneFreeAndNoneOutranked_IsDenied()
        {
            Assert.That(policy.Decide(Low, null, new[] { Flown(Drone1, Middle) }).Denial, Is.EqualTo(FlightDenial.NoDroneAvailable));
            Assert.That(policy.Decide(Low, null, new DroneSnapshot[0]).Denial, Is.EqualTo(FlightDenial.NoDroneAvailable));
        }
    }
}
