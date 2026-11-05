using System;
using System.Globalization;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;
using NUnit.Framework;

namespace DroneFlightModule.UI.Tests
{
    public class FleetDashboardReadoutTests
    {
        private TestFleet fleet;
        private FleetEventLog log;
        private FleetDashboardReadout readout;
        private CultureInfo previousCulture;

        [SetUp]
        public void SetUp()
        {
            // A comma-decimal culture, so a missing invariant format shows up as "2,50".
            previousCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            fleet = new TestFleet();
            log = new FleetEventLog(fleet.Coordinator, fleet.Clock, capacity: 20);
            readout = new FleetDashboardReadout(
                fleet.Drones, fleet.Coordinator, fleet.Pilots, fleet.Stats, fleet.Telemetry, fleet.Terrain, log);
        }

        [TearDown]
        public void TearDown()
        {
            CultureInfo.CurrentCulture = previousCulture;
        }

        private void Report(DroneId drone, FlightMode mode, Vec3 position, AltitudeReading altitude, double holdTimeRemaining = 0.0)
        {
            var state = new DroneState(position, new Vec3(1f, -0.5f, 0f), MathF.PI / 2f, 0f, isGrounded: false);
            fleet.Telemetry.Write(new FlightFrame(drone, 3.0, ControlInput.Zero, ForceCommand.None, state, altitude, mode, holdTimeRemaining));
        }

        [Test]
        public void Constructor_RequiresItsParts()
        {
            Assert.Throws<ArgumentNullException>(() => new FleetDashboardReadout(
                null, fleet.Coordinator, fleet.Pilots, fleet.Stats, fleet.Telemetry, fleet.Terrain, log));
            Assert.Throws<ArgumentNullException>(() => new FleetDashboardReadout(
                fleet.Drones, fleet.Coordinator, fleet.Pilots, fleet.Stats, fleet.Telemetry, fleet.Terrain, null));
        }

        [Test]
        public void AFreeDroneWithoutTelemetry_SaysSo()
        {
            var text = readout.Drones();

            StringAssert.StartsWith("drone-1   free, Unauthorized\n  no telemetry yet", text);
        }

        [Test]
        public void AFlownDrone_ShowsItsPilotAndTelemetry()
        {
            fleet.Fly(TestFleet.Ace.Id, TestFleet.Drone1);
            Report(TestFleet.Drone1, FlightMode.Piloted, new Vec3(1f, 3.5f, 2f), new AltitudeReading(2.5f, true));

            var text = readout.Drones();

            StringAssert.Contains("drone-1   ace (priority 50), Granted", text);
            StringAssert.Contains("Piloted, height 2.50 m (true 2.50 m), speed 1.12 m/s", text);
            StringAssert.Contains("position (1.0, 3.5, 2.0), velocity (1.0, -0.5, 0.0), heading 90°", text);
        }

        [Test]
        public void Holding_ShowsTheCountdown_AndAnOutOfRangeHeight()
        {
            Report(TestFleet.Drone2, FlightMode.Holding, new Vec3(0f, 13f, 0f), AltitudeReading.Invalid, holdTimeRemaining: 7.5);

            StringAssert.Contains("Holding, descending in 7.5 s, height out of range (true 12.00 m)", readout.Drones());
        }

        [Test]
        public void AnEmptyFleet_SaysSo()
        {
            fleet.Coordinator.RemoveDrone(TestFleet.Drone1);
            fleet.Coordinator.RemoveDrone(TestFleet.Drone2);

            Assert.That(readout.Drones(), Is.EqualTo("No drones in the fleet."));
        }

        [Test]
        public void Pilots_HighestPriorityFirst_WithTheirDroneAndStats()
        {
            fleet.Fly(PilotProfile.Guest.Id, TestFleet.Drone2);
            fleet.Stats.Save(PilotProfile.Guest.Id, new PilotStats(2, 31.5, 1, 0, 1.0));

            Assert.That(readout.Pilots(), Is.EqualTo(
                "Ace (ace, priority 50): Unauthorized, not flying, flights 0, 0.0 s, takeovers 0, preempted 0\n" +
                "Guest (guest, priority 0): Granted, flying drone-2, flights 2, 31.5 s, takeovers 1, preempted 0"));
        }

        [Test]
        public void Pilots_ShowARefusedAuthorization()
        {
            fleet.Fly(TestFleet.Ace.Id, TestFleet.Drone1);
            fleet.Server.Revoke(TestFleet.Ace.Id);

            StringAssert.StartsWith("Ace (ace, priority 50): Revoked, not flying", readout.Pilots());
        }

        [Test]
        public void Events_NewestFirst_UpToTheCount()
        {
            Assert.That(readout.Events(5), Is.EqualTo("Nothing yet."));
            fleet.Fly(PilotProfile.Guest.Id, TestFleet.Drone1);
            fleet.Fly(TestFleet.Ace.Id, TestFleet.Drone1);
            fleet.Fly(new PilotId("stranger"), null);

            var text = readout.Events(2);

            Assert.That(text.Split('\n'), Has.Length.EqualTo(2));
            StringAssert.StartsWith("0.0 s  stranger denied", text);
            StringAssert.Contains("ace took over drone-1 from guest", text);
        }
    }
}
