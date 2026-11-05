using DroneFlightModule.Authorization;
using DroneFlightModule.Core;

namespace DroneFlightModule.Coordination.Tests
{
    /// <summary>
    /// A real flight loop gated like the scene's drones: a connected pilot and a valid grant.
    /// </summary>
    internal sealed class TestDrone
    {
        private const float DeltaTime = 0.02f;

        public TestDrone(string id, bool isAirborne)
        {
            Id = new DroneId(id);
            Router = new InputRouter();
            Holder = new DroneGrantHolder(Router);
            Gate = new AllOfFlightGate(new IFlightGate[] { new PilotInControlGate(Router), Holder });

            var terrain = new FlatTerrain();
            var start = isAirborne
                ? new DroneState(new Vec3(0f, 5f, 0f))
                : new DroneState(Vec3.Zero, Vec3.Zero, 0f, 0f, isGrounded: true);
            Loop = new FlightLoop(
                Id,
                Router,
                new VelocityFlightController(DroneConfig.Default, VelocityControllerConfig.Default),
                new FlightSupervisor(Gate, DroneConfig.Default, FailsafeConfig.Default),
                new DroneIntegrator(DroneConfig.Default, terrain),
                new RangefinderAltitudeSensor(terrain, RangefinderConfig.Default),
                start);
            Fleet = new FleetDrone(Id, Router, Holder, new FlightLoopStatus(Loop));
        }

        public DroneId Id { get; }
        public InputRouter Router { get; }
        public DroneGrantHolder Holder { get; }
        public IFlightGate Gate { get; }
        public FlightLoop Loop { get; }
        public FleetDrone Fleet { get; }

        public void Tick()
        {
            Loop.Tick(DeltaTime);
        }
    }
}
