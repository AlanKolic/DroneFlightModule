using System;
using System.Threading;
using DroneFlightModule.Authorization;
using DroneFlightModule.Coordination;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;
using DroneFlightModule.Telemetry;

namespace DroneFlightModule.UI.Tests
{
    /// <summary>
    /// A real coordinator over two drones, the guest and Ace, with stats, a telemetry sink and a
    /// clock the test sets. Both providers answer at once, so every request finishes synchronously.
    /// </summary>
    internal sealed class TestFleet
    {
        public const float GroundHeight = 1f;

        public static readonly DroneId Drone1 = new DroneId("drone-1");
        public static readonly DroneId Drone2 = new DroneId("drone-2");
        public static readonly PilotProfile Ace = new PilotProfile(new PilotId("ace"), "Ace", 50);

        private readonly LocalGuestAuthorizationProvider guest = new LocalGuestAuthorizationProvider(PilotProfile.Guest.Id);
        private readonly SimulatedServerAuthorizationProvider server =
            new SimulatedServerAuthorizationProvider(new[] { Ace.Id }, TimeSpan.Zero, new TaskDelay());

        private sealed class Status : IDroneStatus
        {
            public bool IsGrounded => true;
        }

        private sealed class CentredSticks : IInputProvider
        {
            public bool IsConnected => true;
            public ControlInput Read() => ControlInput.Zero;
        }

        public TestFleet()
        {
            Drones = new DroneRegistry(new[] { Drone(Drone1), Drone(Drone2) });
            Pilots = new InMemoryPilotRegistry(new[] { Ace });
            Coordinator = new FlightCoordinator(Pilots, Drones, new PriorityAssignmentPolicy());
            Stats = new InMemoryPilotStatsStore();
            Telemetry = new InMemoryTelemetrySink();
            Terrain = new FlatTerrain(GroundHeight);
        }

        public DroneRegistry Drones { get; }
        public IPilotRegistry Pilots { get; }
        public FlightCoordinator Coordinator { get; }
        public InMemoryPilotStatsStore Stats { get; }
        public InMemoryTelemetrySink Telemetry { get; }
        public ITerrain Terrain { get; }
        public ManualClock Clock { get; } = new ManualClock();

        public FlightRequestResult Fly(PilotId pilot, DroneId? drone)
        {
            var provider = pilot == Ace.Id ? (IAuthorizationProvider)server : guest;
            var session = new PilotSession(pilot, provider, new CentredSticks());
            return Coordinator.RequestFlightAsync(session, drone, CancellationToken.None).Result;
        }

        private FleetDrone Drone(DroneId id)
        {
            var router = new InputRouter();
            return new FleetDrone(id, router, new DroneGrantHolder(router), new Status());
        }
    }
}
