namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// The outcome of pressing Arm: whether the drone is now armed, and the words for the pilot.
    /// </summary>
    public readonly struct ArmingResult
    {
        public readonly bool IsArmed;
        public readonly string Message;

        public ArmingResult(bool isArmed, string message)
        {
            IsArmed = isArmed;
            Message = message;
        }
    }
}
