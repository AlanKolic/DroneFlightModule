using System.Collections.Generic;
using DroneFlightModule.Authorization;
using DroneFlightModule.Coordination;
using DroneFlightModule.Core;
using DroneFlightModule.Telemetry;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Registers one drone's flight objects in its own child scope; the configs (frame,
    /// controller, rangefinder, failsafe, telemetry), the ground and the fleet come from the fleet
    /// scope. The drone flies whoever the coordinator routes to it, only while that pilot is
    /// connected and holds a valid grant, and
    /// joins the fleet's drone registry once its scope is built, whenever that is. Its loop's time
    /// starts at the fleet's simulation time, so its telemetry lines up with the rest. Its telemetry
    /// recorder starts then too, writing to the fleet's sinks, and ends with the scope.
    /// </summary>
    public sealed class DroneInstaller : IInstaller
    {
        private readonly DroneId droneId;
        private readonly DroneState initialState;
        private readonly double startTime;

        public DroneInstaller(DroneId droneId, DroneState initialState, double startTime)
        {
            this.droneId = droneId;
            this.initialState = initialState;
            this.startTime = startTime;
        }

        public void Install(IContainerBuilder builder)
        {
            builder.Register<IFlightController, VelocityFlightController>(Lifetime.Singleton)
                .WithParameter("gravity", DroneIntegrator.StandardGravity);
            builder.Register<DroneIntegrator>(Lifetime.Singleton)
                .WithParameter("gravity", DroneIntegrator.StandardGravity);
            builder.Register<IAltitudeSensor, RangefinderAltitudeSensor>(Lifetime.Singleton);

            builder.Register<InputRouter>(Lifetime.Singleton);
            builder.Register<PilotInControlGate>(Lifetime.Singleton);
            builder.Register<DroneGrantHolder>(Lifetime.Singleton);
            builder.Register<IFlightGate, AllOfFlightGate>(Lifetime.Singleton)
                .WithParameter<IReadOnlyList<IFlightGate>>(resolver =>
                    new IFlightGate[] { resolver.Resolve<PilotInControlGate>(), resolver.Resolve<DroneGrantHolder>() });
            builder.Register<FlightSupervisor>(Lifetime.Singleton)
                .WithParameter("gravity", DroneIntegrator.StandardGravity);

            builder.Register<FlightLoop>(Lifetime.Singleton)
                .WithParameter("droneId", droneId)
                .WithParameter("initialState", initialState)
                .WithParameter("startTime", startTime)
                .WithParameter<IInputProvider>(resolver => resolver.Resolve<InputRouter>());

            builder.Register<TelemetryRecorder>(Lifetime.Singleton)
                .WithParameter<IReadOnlyList<ITelemetrySink>>(resolver =>
                    new ITelemetrySink[] { resolver.Resolve<InMemoryTelemetrySink>() });

            builder.Register<IDroneStatus, FlightLoopStatus>(Lifetime.Singleton);
            builder.Register<FleetDrone>(Lifetime.Singleton)
                .WithParameter("id", droneId);
            builder.RegisterBuildCallback(container =>
            {
                container.Resolve<TelemetryRecorder>().SinkFailed += (sink, exception) => Debug.LogException(exception);
                container.Resolve<DroneRegistry>().Add(container.Resolve<FleetDrone>());
            });
        }
    }
}
