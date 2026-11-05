using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// The core flight loop of one drone. It has no clock of its own: whoever owns it calls
    /// <see cref="Tick"/> with a fixed step, whether that is Unity, a test or a headless sim.
    /// The supervisor decides each tick whether the pilot's command or the failsafe flies. Time
    /// starts at the given start time, so a drone that joins a running simulation shares its
    /// timebase with the drones already flying.
    /// </summary>
    public sealed class FlightLoop
    {
        private readonly ArgumentGuard guard = new ArgumentGuard();
        private readonly IInputProvider input;
        private readonly IFlightController controller;
        private readonly FlightSupervisor supervisor;
        private readonly DroneIntegrator integrator;
        private readonly IAltitudeSensor altitudeSensor;

        public FlightLoop(
            DroneId droneId,
            IInputProvider input,
            IFlightController controller,
            FlightSupervisor supervisor,
            DroneIntegrator integrator,
            IAltitudeSensor altitudeSensor,
            DroneState initialState,
            double startTime = 0.0)
        {
            DroneId = guard.RequireId(droneId, nameof(droneId));
            this.input = input ?? throw new ArgumentNullException(nameof(input));
            this.controller = controller ?? throw new ArgumentNullException(nameof(controller));
            this.supervisor = supervisor ?? throw new ArgumentNullException(nameof(supervisor));
            this.integrator = integrator ?? throw new ArgumentNullException(nameof(integrator));
            this.altitudeSensor = altitudeSensor ?? throw new ArgumentNullException(nameof(altitudeSensor));
            State = initialState;
            if (!(startTime >= 0.0) || double.IsInfinity(startTime))
            {
                throw new ArgumentOutOfRangeException(nameof(startTime), startTime, "Must be non-negative and finite.");
            }
            Time = startTime;
        }

        public DroneId DroneId { get; }
        public DroneState State { get; private set; }
        public FlightMode Mode => supervisor.Mode;
        public double Time { get; private set; }
        public event Action<FlightFrame> Ticked;

        public void Tick(float deltaTime)
        {
            guard.RequirePositive(deltaTime, nameof(deltaTime));

            var altitude = altitudeSensor.Read(State, Time);
            var currentInput = input.Read();
            var pilotCommand = controller.Compute(currentInput, State);
            var command = supervisor.Supervise(pilotCommand, State, altitude, deltaTime);
            State = integrator.Step(State, command, deltaTime);
            Time += deltaTime;

            Ticked?.Invoke(new FlightFrame(
                DroneId,
                Time,
                currentInput,
                command,
                State,
                altitude,
                supervisor.Mode,
                supervisor.HoldTimeRemaining));
        }
    }
}
