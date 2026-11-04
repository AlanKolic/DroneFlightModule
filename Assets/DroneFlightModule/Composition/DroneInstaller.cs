using System.Collections.Generic;
using DroneFlightModule.Authorization;
using DroneFlightModule.Coordination;
using DroneFlightModule.Core;
using VContainer;
using VContainer.Unity;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Registers one drone's flight objects in its own child scope; the configs, the ground, the
    /// pilot claims and the fleet come from the fleet scope. The drone flies whoever the
    /// coordinator routes to it, only while that pilot is connected and holds a valid grant, and
    /// joins the fleet's drone registry once its scope is built, whenever that is.
    /// </summary>
    public sealed class DroneInstaller : IInstaller
    {
        private readonly DroneId droneId;
        private readonly DroneState initialState;

        public DroneInstaller(DroneId droneId, DroneState initialState)
        {
            this.droneId = droneId;
            this.initialState = initialState;
        }

        public void Install(IContainerBuilder builder)
        {
            builder.Register<IFlightController, VelocityFlightController>(Lifetime.Singleton)
                .WithParameter("gravity", DroneIntegrator.StandardGravity);
            builder.Register<DroneIntegrator>(Lifetime.Singleton)
                .WithParameter("gravity", DroneIntegrator.StandardGravity);
            builder.Register<IAltitudeSensor, RangefinderAltitudeSensor>(Lifetime.Singleton)
                .WithParameter("maxRange", RangefinderAltitudeSensor.DefaultMaxRange)
                .WithParameter("updateRate", RangefinderAltitudeSensor.DefaultUpdateRate);

            builder.Register<InputRouter>(Lifetime.Singleton);
            builder.Register<PilotInControlGate>(Lifetime.Singleton);
            builder.Register<DroneGrantHolder>(Lifetime.Singleton)
                .WithParameter("drone", droneId);
            builder.Register<IFlightGate, AllOfFlightGate>(Lifetime.Singleton)
                .WithParameter<IReadOnlyList<IFlightGate>>(resolver =>
                    new IFlightGate[] { resolver.Resolve<PilotInControlGate>(), resolver.Resolve<DroneGrantHolder>() });
            builder.Register<FlightSupervisor>(Lifetime.Singleton)
                .WithParameter("gravity", DroneIntegrator.StandardGravity);

            builder.Register<FlightLoop>(Lifetime.Singleton)
                .WithParameter("droneId", droneId)
                .WithParameter("initialState", initialState)
                .WithParameter<IInputProvider>(resolver => resolver.Resolve<InputRouter>());

            builder.Register<IDroneStatus, FlightLoopStatus>(Lifetime.Singleton);
            builder.Register<FleetDrone>(Lifetime.Singleton)
                .WithParameter("id", droneId);
            builder.RegisterBuildCallback(container =>
                container.Resolve<DroneRegistry>().Add(container.Resolve<FleetDrone>()));
        }
    }
}
