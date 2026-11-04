using System;
using DroneFlightModule.Coordination;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;
using NUnit.Framework;

namespace DroneFlightModule.UI.Tests
{
    public class ControlPanelChoicesTests
    {
        private TestFleet fleet;
        private ControlPanelChoices choices;

        [SetUp]
        public void SetUp()
        {
            fleet = new TestFleet();
            choices = new ControlPanelChoices(fleet.Pilots, fleet.Drones, fleet.Coordinator, fleet.Stats);
        }

        [Test]
        public void Constructor_RequiresThePilotsTheFleetTheCoordinatorAndTheStats()
        {
            Assert.Throws<ArgumentNullException>(() => new ControlPanelChoices(null, fleet.Drones, fleet.Coordinator, fleet.Stats));
            Assert.Throws<ArgumentNullException>(() => new ControlPanelChoices(fleet.Pilots, null, fleet.Coordinator, fleet.Stats));
            Assert.Throws<ArgumentNullException>(() => new ControlPanelChoices(fleet.Pilots, fleet.Drones, null, fleet.Stats));
            Assert.Throws<ArgumentNullException>(() => new ControlPanelChoices(fleet.Pilots, fleet.Drones, fleet.Coordinator, null));
        }

        [Test]
        public void OffersEveryPilot_HighestPriorityFirst_AndEveryDroneInOrder()
        {
            Assert.That(choices.Pilots[0].Id, Is.EqualTo(TestFleet.Ace.Id));
            Assert.That(choices.Pilots[1].Id, Is.EqualTo(PilotProfile.Guest.Id));
            Assert.That(choices.Drones[0].Id, Is.EqualTo(TestFleet.Drone1));
            Assert.That(choices.Drones[1].Id, Is.EqualTo(TestFleet.Drone2));
        }

        [Test]
        public void StartsOnAnyDrone_AndRemembersTheChoice()
        {
            Assert.That(choices.SelectedDrone, Is.Null);

            choices.SelectDrone(TestFleet.Drone2);
            Assert.That(choices.SelectedDrone, Is.EqualTo(TestFleet.Drone2));

            choices.SelectDrone(null);
            Assert.That(choices.SelectedDrone, Is.Null);
        }

        [Test]
        public void ADroneOutsideTheFleet_CannotBeChosen()
        {
            Assert.Throws<ArgumentException>(() => choices.SelectDrone(new DroneId("drone-9")));
        }

        [Test]
        public void AChosenDroneThatLeaves_ReadsAsAnyDrone()
        {
            choices.SelectDrone(TestFleet.Drone2);

            fleet.Coordinator.RemoveDrone(TestFleet.Drone2);

            Assert.That(choices.SelectedDrone, Is.Null);
        }

        [Test]
        public void Labels_NameTheDroneOrAny_AndThePilotWithTheirPriority()
        {
            Assert.That(choices.Label((DroneId?)null), Is.EqualTo("Any drone"));
            Assert.That(choices.Label(TestFleet.Drone1), Is.EqualTo("drone-1, free"));
            Assert.That(choices.Label(TestFleet.Ace.Id), Is.EqualTo("Ace (priority 50)"));
            Assert.That(choices.Label(new PilotId("stranger")), Is.EqualTo("stranger"));
        }

        [Test]
        public void ATakeover_ShowsInBothLabels()
        {
            fleet.Fly(PilotProfile.Guest.Id, TestFleet.Drone1);
            Assert.That(choices.Label(TestFleet.Drone1), Is.EqualTo("drone-1, flown by guest"));
            Assert.That(choices.Label(PilotProfile.Guest.Id), Is.EqualTo("Guest (priority 0), flying drone-1"));

            var result = fleet.Fly(TestFleet.Ace.Id, TestFleet.Drone1);

            Assert.That(result.Outcome, Is.EqualTo(FlightRequestOutcome.TookOver));
            Assert.That(choices.Label(TestFleet.Drone1), Is.EqualTo("drone-1, flown by ace"));
            Assert.That(choices.Label(TestFleet.Ace.Id), Is.EqualTo("Ace (priority 50), flying drone-1"));
            Assert.That(choices.Label(PilotProfile.Guest.Id), Is.EqualTo("Guest (priority 0)"));
        }

        [Test]
        public void Record_CountsTakeoversAndPreemptions()
        {
            fleet.Stats.Save(TestFleet.Ace.Id, fleet.Stats.Get(TestFleet.Ace.Id).WithTakeover());
            fleet.Stats.Save(PilotProfile.Guest.Id, fleet.Stats.Get(PilotProfile.Guest.Id).WithPreemption());

            Assert.That(choices.Record(TestFleet.Ace.Id), Is.EqualTo("takeovers 1, preempted 0"));
            Assert.That(choices.Record(PilotProfile.Guest.Id), Is.EqualTo("takeovers 0, preempted 1"));
        }
    }
}
