using System;
using DroneFlightModule.Pilots;

namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// Records pilot stats from the coordinator's events, timed by the simulation clock. A
    /// takeover ends the preempted pilot's flight and starts the new pilot's at the same moment.
    /// Create it before any flight starts, so every flight it ends is one it saw begin. Disposing
    /// stops recording.
    /// </summary>
    public sealed class PilotStatsListener : IDisposable
    {
        private readonly FlightCoordinator coordinator;
        private readonly PilotStatsRecorder recorder;
        private readonly ISimulationClock clock;

        public PilotStatsListener(FlightCoordinator coordinator, PilotStatsRecorder recorder, ISimulationClock clock)
        {
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            this.recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            coordinator.Assigned += OnAssigned;
            coordinator.Released += OnReleased;
            coordinator.TookOver += OnTookOver;
        }

        public void Dispose()
        {
            coordinator.Assigned -= OnAssigned;
            coordinator.Released -= OnReleased;
            coordinator.TookOver -= OnTookOver;
        }

        private void OnAssigned(DroneAssigned assigned)
        {
            recorder.FlightStarted(assigned.Pilot, assigned.Drone, clock.Time);
        }

        private void OnReleased(DroneReleased released)
        {
            recorder.FlightEnded(released.Pilot, released.Drone, clock.Time);
        }

        private void OnTookOver(DroneTakenOver takenOver)
        {
            double time = clock.Time;
            recorder.FlightEnded(takenOver.FromPilot, takenOver.Drone, time);
            recorder.Preempted(takenOver.FromPilot);
            recorder.TookOver(takenOver.Pilot);
            recorder.FlightStarted(takenOver.Pilot, takenOver.Drone, time);
        }
    }
}
