using DroneFlightModule.Core;
using NUnit.Framework;

namespace DroneFlightModule.Unity.Tests
{
    public class FlightReadoutTests
    {
        private static readonly PilotId Pilot = new PilotId("local-pilot");
        private static readonly DroneId Drone = new DroneId("drone-1");

        private readonly FlightReadout readout = new FlightReadout();

        private FlightFrame Frame(FlightMode mode, double holdTimeRemaining, AltitudeReading altitude, float yaw = 0f)
        {
            var state = new DroneState(new Vec3(0f, 3f, 0f), new Vec3(3f, -1f, 4f), yaw, 0f, isGrounded: false);
            return new FlightFrame(Drone, 1.0, ControlInput.Zero, ForceCommand.None, state, altitude, mode, holdTimeRemaining);
        }

        [Test]
        public void Holding_ShowsCountdown()
        {
            var text = readout.Format(Frame(FlightMode.Holding, 7.42, new AltitudeReading(3f, true)), Pilot, false);

            StringAssert.Contains("Mode: Holding, descending in 7.4 s", text);
            StringAssert.Contains("Pilot: local-pilot (link lost)", text);
        }

        [Test]
        public void Piloted_ShowsHeightSpeedAndHeading()
        {
            var text = readout.Format(Frame(FlightMode.Piloted, 0.0, new AltitudeReading(3.256f, true), yaw: -1.5707964f), Pilot, true);

            StringAssert.Contains("Mode: Piloted", text);
            StringAssert.Contains("Pilot: local-pilot (connected)", text);
            StringAssert.Contains("Height: 3.26 m", text);
            StringAssert.Contains("Speed: 5.0 m/s, vertical -1.0 m/s", text);
            StringAssert.Contains("Heading: 270°", text);
        }

        [Test]
        public void YawJustLeftOfNorth_ShowsZeroHeading()
        {
            var text = readout.Format(Frame(FlightMode.Piloted, 0.0, new AltitudeReading(3f, true), yaw: -0.0003f), Pilot, true);

            StringAssert.Contains("Heading: 0°", text);
        }

        [Test]
        public void InvalidReading_ShowsOutOfRange_NoPilot_ShowsNone()
        {
            var text = readout.Format(Frame(FlightMode.Descending, 0.0, AltitudeReading.Invalid), default, false);

            StringAssert.Contains("Height: out of range", text);
            StringAssert.Contains("Pilot: none", text);
        }
    }
}
