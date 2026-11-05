using System;
using System.Globalization;
using DroneFlightModule.Core;

namespace DroneFlightModule.UI
{
    /// <summary>
    /// How the dashboard words a flight: mode with the failsafe countdown, measured height, speed
    /// and heading. Invariant culture keeps the decimal point stable.
    /// </summary>
    public sealed class FlightTextFormat
    {
        private const double RadiansToDegrees = 180.0 / Math.PI;

        public string Mode(in FlightFrame frame)
        {
            return frame.Mode == FlightMode.Holding
                ? Invariant($"Holding, descending in {frame.HoldTimeRemaining:0.0} s")
                : frame.Mode.ToString();
        }

        public string Height(in AltitudeReading altitude)
        {
            return altitude.IsValid ? Invariant($"{altitude.HeightAboveGround:0.00} m") : "out of range";
        }

        public string Speed(float metersPerSecond)
        {
            return Invariant($"{metersPerSecond:0.00} m/s");
        }

        public string Heading(float yaw)
        {
            // Rounded before wrapping, so a yaw just left of north shows 0°, never 360°.
            double heading = (Math.Round(yaw * RadiansToDegrees) % 360.0 + 360.0) % 360.0;
            return Invariant($"{heading:0}°");
        }

        public string Invariant(FormattableString text)
        {
            return text.ToString(CultureInfo.InvariantCulture);
        }
    }
}
