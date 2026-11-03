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

        public PilotStats WithFlightStarted(double time) =>
            new PilotStats(Flights + 1, FlightTime, Takeovers, TimesPreempted, time);

        public PilotStats WithFlightTime(double duration) =>
            new PilotStats(Flights, FlightTime + duration, Takeovers, TimesPreempted, LastFlight);

        public PilotStats WithTakeover() =>
            new PilotStats(Flights, FlightTime, Takeovers + 1, TimesPreempted, LastFlight);

        public PilotStats WithPreemption() =>
            new PilotStats(Flights, FlightTime, Takeovers, TimesPreempted + 1, LastFlight);

        public override string ToString() =>
            $"flights {Flights} time {FlightTime:0.#} s takeovers {Takeovers} preempted {TimesPreempted}";
    }
}
