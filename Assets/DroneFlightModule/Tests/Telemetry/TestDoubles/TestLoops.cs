using DroneFlightModule.Core;

namespace DroneFlightModule.Telemetry.Tests
{
    /// <summary>
    /// A real flight loop that hovers on its own: centred sticks and a gate that is always open.
    /// </summary>
    internal sealed class TestLoops
    {
        public const float DeltaTime = 0.02f;

        private sealed class CentredSticks : IInputProvider
        {
            public bool IsConnected => true;
            public ControlInput Read() => ControlInput.Zero;
        }

        private sealed class OpenGate : IFlightGate
        {
            public bool IsFlightAllowed => true;
        }

        public FlightLoop Hovering(string id)
        {
            var terrain = new FlatTerrain();
            return new FlightLoop(
                new DroneId(id),
                new CentredSticks(),
                new VelocityFlightController(DroneConfig.Default, VelocityControllerConfig.Default),
                new FlightSupervisor(new OpenGate(), new ArmingSwitch(), DroneConfig.Default, FailsafeConfig.Default),
                new DroneIntegrator(DroneConfig.Default, terrain),
                new RangefinderAltitudeSensor(terrain, RangefinderConfig.Default),
                new DroneState(new Vec3(0f, 5f, 0f)));
        }

        public void Run(FlightLoop loop, int ticks)
        {
            for (int i = 0; i < ticks; i++)
            {
                loop.Tick(DeltaTime);
            }
        }
    }
}
