using DroneFlightModule.Core;
using NUnit.Framework;

namespace DroneFlightModule.UI.Tests
{
    public class FlightTextFormatTests
    {
        private readonly FlightTextFormat format = new FlightTextFormat();

        private FlightFrame Frame(FlightMode mode, double holdTimeRemaining)
        {
            return new FlightFrame(
                new DroneId("drone-1"), 0.0, ControlInput.Zero, ForceCommand.None, default, AltitudeReading.Invalid, mode, holdTimeRemaining);
        }

        [Test]
        public void Mode_GivesTheCountdownOnlyWhileHolding()
        {
            Assert.That(format.Mode(Frame(FlightMode.Holding, 7.25)), Is.EqualTo("Holding, descending in 7.2 s").Or.EqualTo("Holding, descending in 7.3 s"));
            Assert.That(format.Mode(Frame(FlightMode.Piloted, 0.0)), Is.EqualTo("Piloted"));
        }

        [Test]
        public void Height_SaysWhenItIsOutOfRange()
        {
            Assert.That(format.Height(new AltitudeReading(2.5f, true)), Is.EqualTo("2.50 m"));
            Assert.That(format.Height(AltitudeReading.Invalid), Is.EqualTo("out of range"));
        }

        [TestCase(0f, "0°")]
        [TestCase(1.5707964f, "90°")]
        [TestCase(-1.5707964f, "270°")]
        [TestCase(-0.001f, "0°")]
        public void Heading_IsWholeDegreesFromZeroTo359(float yaw, string expected)
        {
            Assert.That(format.Heading(yaw), Is.EqualTo(expected));
        }
    }
}
