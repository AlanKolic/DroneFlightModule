namespace DroneFlightModule.Core
{
    /// <summary>
    /// One condition for flight. The flight loop only asks whether flight is allowed right now,
    /// never why; new conditions are new gates combined in the composition root.
    /// </summary>
    public interface IFlightGate
    {
        bool IsFlightAllowed { get; }
    }
}
