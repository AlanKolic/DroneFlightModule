using System;
using System.Globalization;
using DroneFlightModule.Core;

namespace DroneFlightModule.Unity
{
    /// <summary>
    /// Turns a flight frame into the debug HUD's text: mode and failsafe countdown, pilot link,
    /// measured height, speed and heading. Invariant culture keeps the decimal point stable.
    /// </summary>
    public sealed class FlightReadout
    {
        private const float RadiansToDegrees = 180f / MathF.PI;

        public string Format(in FlightFrame frame, PilotId pilot, bool pilotConnected)
        {
            var state = frame.State;
            var horizontalSpeed = new Vec3(state.Velocity.X, 0f, state.Velocity.Z).Magnitude;
            // Rounded before wrapping, so a yaw just left of north shows 0°, never 360°.
            float heading = (MathF.Round(state.Yaw * RadiansToDegrees) % 360f + 360f) % 360f;

            return string.Join("\n",
                $"Mode: {Mode(frame)}",
                $"Pilot: {Pilot(pilot, pilotConnected)}",
                $"Height: {Height(frame.Altitude)}",
                Invariant($"Speed: {horizontalSpeed:0.0} m/s, vertical {state.Velocity.Y:0.0} m/s"),
                Invariant($"Heading: {heading:0}°"));
        }

        private string Mode(in FlightFrame frame)
        {
            if (frame.Mode == FlightMode.Holding)
            {
                return Invariant($"Holding, descending in {frame.HoldTimeRemaining:0.0} s");
            }
            return frame.Mode.ToString();
        }

        private string Pilot(PilotId pilot, bool connected)
        {
            if (pilot.IsEmpty)
            {
                return "none";
            }
            return connected ? $"{pilot} (connected)" : $"{pilot} (link lost)";
        }

        private string Height(in AltitudeReading altitude)
        {
            return altitude.IsValid ? Invariant($"{altitude.HeightAboveGround:0.00} m") : "out of range";
        }

        private string Invariant(FormattableString text)
        {
            return text.ToString(CultureInfo.InvariantCulture);
        }
    }
}
