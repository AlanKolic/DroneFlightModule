namespace DroneFlightModule.Core
{
    /// <summary>
    /// A source of pilot input: keyboard, phone, scripted, etc. IsConnected is false while the
    /// source can't reach its pilot, such as a phone whose link has dropped.
    /// </summary>
    public interface IInputProvider
    {
        bool IsConnected { get; }

        ControlInput Read();
    }
}
