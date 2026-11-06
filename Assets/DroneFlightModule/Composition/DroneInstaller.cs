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
    /// Registers one drone's flight objects in its own child scope; everything shared comes from the
    /// fleet scope. The drone flies whoever the coordinator routes to it, and joins the registry once
    /// its scope is built. Its loop and telemetry start at the fleet's simulation time, so its samples
    /// line up with the rest.
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

            builder.Register<InputRouter>(Lifetime.Singleton).AsSelf().As<IInputProvider>();
            builder.Register<PilotInControlGate>(Lifetime.Singleton);
            builder.Register<DroneGrantHolder>(Lifetime.Singleton);
            builder.Register<IFlightGate, AllOfFlightGate>(Lifetime.Singleton)
                .WithParameter<IReadOnlyList<IFlightGate>>(resolver =>
                    new IFlightGate[] { resolver.Resolve<PilotInControlGate>(), resolver.Resolve<DroneGrantHolder>() });
            builder.Register<ArmingSwitch>(Lifetime.Singleton);
            builder.Register<FlightSupervisor>(Lifetime.Singleton)
                .WithParameter("gravity", DroneIntegrator.StandardGravity);

            builder.Register<FlightLoop>(Lifetime.Singleton)
                .WithParameter("droneId", droneId)
                .WithParameter("initialState", initialState)
                .WithParameter("startTime", startTime);

            builder.Register<TelemetryRecorder>(Lifetime.Singleton);

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
