namespace DroneFlightModule.Unity
{
    /// <summary>
    /// One line about a pilot station for the debug HUD: its name, who flies from it, their drone
    /// and its link.
    /// </summary>
    public interface IStationStatus
    {
        string Describe();
    }
}
