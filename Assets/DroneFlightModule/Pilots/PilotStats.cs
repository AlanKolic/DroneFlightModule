namespace DroneFlightModule.Pilots
{
    /// <summary>
    /// One pilot's flying record. FlightTime counts finished flights only, in simulation seconds;
    /// LastFlight is the simulation time the latest flight started, or null before the first one.
    /// </summary>
    public readonly struct PilotStats
    {
        public static readonly PilotStats None = default;

        public readonly int Flights;
        public readonly double FlightTime;
        public readonly int Takeovers;
        public readonly int TimesPreempted;
        public readonly double? LastFlight;

        public PilotStats(int flights, double flightTime, int takeovers, int timesPreempted, double? lastFlight)
        {
            Flights = flights;
            FlightTime = flightTime;
            Takeovers = takeovers;
            TimesPreempted = timesPreempted;
            LastFlight = lastFlight;
        }

        public override string ToString() =>
            $"flights {Flights} time {FlightTime:0.#} s takeovers {Takeovers} preempted {TimesPreempted}";
    }
}
