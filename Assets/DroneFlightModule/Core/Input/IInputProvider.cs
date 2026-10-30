namespace DroneFlightModule.Core
{
    /// <summary>
    /// A source of pilot input: keyboard, phone, scripted, etc.
    /// </summary>
    public interface IInputProvider
    {
        ControlInput Read();
    }
}
