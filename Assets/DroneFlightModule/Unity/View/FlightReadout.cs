using System;
using DroneFlightModule.Core;

namespace DroneFlightModule.Unity
{
    /// <summary>
    /// Turns a flight frame into the debug HUD's text: mode and failsafe countdown, pilot link,
    /// measured height, speed and heading.
    /// </summary>
    public sealed class FlightReadout
    {
        private readonly FlightTextFormat format = new FlightTextFormat();

        public string Format(in FlightFrame frame, PilotId pilot, bool pilotConnected)
        {
            var state = frame.State;
            var horizontalSpeed = new Vec3(state.Velocity.X, 0f, state.Velocity.Z).Magnitude;

            return string.Join("\n",
                $"Mode: {format.Mode(frame)}",
                $"Pilot: {Pilot(pilot, pilotConnected)}",
                $"Height: {format.Height(frame.Altitude)}",
                format.Invariant($"Speed: {horizontalSpeed:0.0} m/s, vertical {state.Velocity.Y:0.0} m/s"),
                $"Heading: {format.Heading(state.Yaw)}");
        }

        private string Pilot(PilotId pilot, bool connected)
        {
            if (pilot.IsEmpty)
            {
                return "none";
            }
            return connected ? $"{pilot} (connected)" : $"{pilot} (link lost)";
        }
    }
}
