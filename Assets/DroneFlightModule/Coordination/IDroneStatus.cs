namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// What the coordinator needs to know about a drone's flight when choosing one for a pilot.
    /// </summary>
    public interface IDroneStatus
    {
        bool IsGrounded { get; }
    }
}
