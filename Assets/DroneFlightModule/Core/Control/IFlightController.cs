namespace DroneFlightModule.Core
{
    /// <summary>
    /// Turns pilot intent into actuator demand.
    /// </summary>
    public interface IFlightController
    {
        ForceCommand Compute(in ControlInput input, in DroneState state);
    }
}
