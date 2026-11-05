namespace DroneFlightModule.Core
{
    /// <summary>
    /// A source of pilot input: a keyboard, a gamepad, a script. IsConnected is false while the
    /// source can't reach its pilot, such as an unplugged gamepad.
    /// </summary>
    public interface IInputProvider
    {
        bool IsConnected { get; }

        ControlInput Read();
    }
}
