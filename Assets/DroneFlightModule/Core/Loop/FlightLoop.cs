using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// The core flight loop of one drone. It has no clock of its own: whoever owns it calls
    /// <see cref="Tick"/> with a fixed step, whether that is Unity, a test or a headless sim.
    /// </summary>
    public sealed class FlightLoop
    {
        private readonly IInputProvider input;
        private readonly IFlightController controller;
        private readonly DroneIntegrator integrator;
        private readonly IAltitudeSensor altitudeSensor;

        public FlightLoop(
            DroneId droneId,
            IInputProvider input,
            IFlightController controller,
            DroneIntegrator integrator,
            IAltitudeSensor altitudeSensor,
            DroneState initialState)
        {
            if (droneId.IsEmpty)
            {
                throw new ArgumentException("Drone id is required.", nameof(droneId));
            }
            DroneId = droneId;
            this.input = input ?? throw new ArgumentNullException(nameof(input));
            this.controller = controller ?? throw new ArgumentNullException(nameof(controller));
            this.integrator = integrator ?? throw new ArgumentNullException(nameof(integrator));
            this.altitudeSensor = altitudeSensor ?? throw new ArgumentNullException(nameof(altitudeSensor));
            State = initialState;
        }

        public DroneId DroneId { get; }
        public DroneState State { get; private set; }
        public double Time { get; private set; }
        public event Action<FlightFrame> Ticked;

        public void Tick(float deltaTime)
        {
            if (!(deltaTime > 0f) || float.IsInfinity(deltaTime))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Tick duration must be positive and finite.");    
            }

            var altitude = altitudeSensor.Read(State, Time);
            var currentInput = input.Read();
            var command = controller.Compute(currentInput, State);
            State = integrator.Step(State, command, deltaTime);
            Time += deltaTime;

            Ticked?.Invoke(new FlightFrame(DroneId, Time, currentInput, command, State, altitude));
        }
    }
}
