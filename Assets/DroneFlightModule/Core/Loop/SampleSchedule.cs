namespace DroneFlightModule.Core
{
    /// <summary>
    /// Says when something sampled at a fixed rate of simulation time is due, starting with the
    /// first time it is asked. The schedule advances by whole periods, so a tick step that does not
    /// divide the period still averages the rate; a gap longer than a period starts it again
    /// instead of sampling to catch up.
    /// </summary>
    public sealed class SampleSchedule
    {
        // Absorbs float drift when the tick step divides the sample period exactly.
        private const double TimeTolerance = 1e-6;

        private readonly ArgumentGuard guard = new ArgumentGuard();
        private readonly double period;
        private bool hasStarted;
        private double nextTime;

        public SampleSchedule(float rate)
        {
            period = 1.0 / guard.RequirePositive(rate, nameof(rate));
        }

        public bool IsDue(double time)
        {
            if (hasStarted && time < nextTime - TimeTolerance)
            {
                return false;
            }
            nextTime = hasStarted ? nextTime + period : time + period;
            if (nextTime <= time)
            {
                nextTime = time + period;
            }
            hasStarted = true;
            return true;
        }
    }
}
