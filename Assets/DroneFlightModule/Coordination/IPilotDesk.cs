using DroneFlightModule.Core;

namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// What seating needs from a desk: its name, the input a pilot flies with there, who sits there,
    /// and a place for the outcome. Whether a device or a script is behind it is the desk's business.
    /// </summary>
    public interface IPilotDesk
    {
        string Name { get; }
        IInputProvider Input { get; }
        PilotSession Session { get; }

        void Occupy(PilotSession session);

        void Report(string message);
    }
}
