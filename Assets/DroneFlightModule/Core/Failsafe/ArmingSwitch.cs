namespace DroneFlightModule.Core
{
    /// <summary>
    /// One per drone. Whether the pilot wants the motors on while the drone is on the ground. The
    /// supervisor reads it there and clears it; in the air it plays no part.
    /// </summary>
    public sealed class ArmingSwitch
    {
        public bool IsArmed { get; private set; }

        public void Arm()
        {
            IsArmed = true;
        }

        public void Disarm()
        {
            IsArmed = false;
        }
    }
}
