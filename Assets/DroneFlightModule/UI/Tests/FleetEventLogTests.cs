using System;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;
using NUnit.Framework;

namespace DroneFlightModule.UI.Tests
{
    public class FleetEventLogTests
    {
        private TestFleet fleet;
        private FleetEventLog log;

        [SetUp]
        public void SetUp()
        {
            fleet = new TestFleet();
            log = new FleetEventLog(fleet.Coordinator, fleet.Clock, capacity: 10);
        }

        [Test]
        public void Constructor_RequiresItsParts()
        {
            Assert.Throws<ArgumentNullException>(() => new FleetEventLog(null, fleet.Clock, 10));
            Assert.Throws<ArgumentNullException>(() => new FleetEventLog(fleet.Coordinator, null, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FleetEventLog(fleet.Coordinator, fleet.Clock, 0));
        }

        [Test]
        public void RecordsEachEvent_StampedWithSimulationTime()
        {
            fleet.Clock.Time = 1.0;
            fleet.Fly(PilotProfile.Guest.Id, TestFleet.Drone1);
            fleet.Clock.Time = 2.5;
            fleet.Fly(TestFleet.Ace.Id, TestFleet.Drone1);
            fleet.Fly(new PilotId("stranger"), null);
            fleet.Coordinator.RemoveDrone(TestFleet.Drone1);

            Assert.That(log.Lines, Is.EqualTo(new[]
            {
                "1.0 s  guest got drone-1",
                "2.5 s  ace took over drone-1 from guest",
                "2.5 s  stranger denied: stranger is not a registered pilot.",
                "2.5 s  drone-1 left the fleet, so ace lost it"
            }));
        }

        [Test]
        public void KeepsOnlyTheLatestLines()
        {
            var shortLog = new FleetEventLog(fleet.Coordinator, fleet.Clock, capacity: 2);

            for (int i = 0; i < 3; i++)
            {
                fleet.Fly(new PilotId($"stranger-{i}"), null);
            }

            Assert.That(shortLog.Lines, Has.Count.EqualTo(2));
            Assert.That(shortLog.Lines[0], Does.Contain("stranger-1"));
        }

        [Test]
        public void AfterDispose_NothingIsRecorded()
        {
            log.Dispose();

            fleet.Fly(PilotProfile.Guest.Id, null);

            Assert.That(log.Lines, Is.Empty);
        }
    }
}
