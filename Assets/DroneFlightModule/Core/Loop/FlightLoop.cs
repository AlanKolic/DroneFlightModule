using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// The core flight loop of one drone. It has no clock of its own: whoever owns it calls
    /// <see cref="Tick"/> with a fixed step, whether that is Unity, a test or a headless sim.
    /// </summary>
    public sealed class FlightLoop
    {
        private readonly IInputProvider _input;
        private readonly IFlightController _controller;
        private readonly DroneIntegrator _integrator;

        public FlightLoop(
            DroneId droneId,
            IInputProvider input,
            IFlightController controller,
            DroneIntegrator integrator,
            DroneState initialState)
        {
            if (droneId.IsEmpty)
            {
                throw new ArgumentException("Drone id is required.", nameof(droneId));
            }
            DroneId = droneId;
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _integrator = integrator ?? throw new ArgumentNullException(nameof(integrator));
            State = initialState;
        }

        public DroneId DroneId { get; }
        public DroneState State { get; private set; }
        public double Time { get; private set; }
        public event Action<FlightFrame> Ticked;

        public void Tick(float dt)
        {
            if (!(dt > 0f) || float.IsInfinity(dt))
            {
                throw new ArgumentOutOfRangeException(nameof(dt), dt, "Tick duration must be positive and finite.");    
            }

            var input = _input.Read();
            var command = _controller.Compute(input, State);
            State = _integrator.Step(State, command, dt);
            Time += dt;

            Ticked?.Invoke(new FlightFrame(DroneId, Time, input, command, State));
        }
    }
}
