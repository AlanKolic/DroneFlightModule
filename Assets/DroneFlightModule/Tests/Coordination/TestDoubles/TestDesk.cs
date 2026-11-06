using DroneFlightModule.Core;

namespace DroneFlightModule.Coordination.Tests
{
    /// <summary>
    /// A desk with nothing behind it: it keeps the session it is given and the last report.
    /// </summary>
    internal sealed class TestDesk : IPilotDesk
    {
        public TestDesk(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public IInputProvider Input { get; } = new ScriptedInput();
        public PilotSession Session { get; private set; }
        public string LastReport { get; private set; }

        public void Occupy(PilotSession session)
        {
            Session = session;
        }

        public void Report(string message)
        {
            LastReport = message;
        }
    }
}
