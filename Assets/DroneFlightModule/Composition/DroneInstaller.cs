using DroneFlightModule.Core;
using DroneFlightModule.Unity;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Registers one drone's Core flight objects with plain constructors. Kept apart from the
    /// scope so tests can build the container without a scene. Until the flight coordinator
    /// exists, the local pilot (keyboard or gamepad, through a link that can be cut for testing)
    /// is assigned to the drone directly.
    /// </summary>
    public sealed class DroneInstaller : IInstaller
    {
        public static readonly PilotId LocalPilot = new PilotId("local-pilot");

        private readonly DroneId droneId;
        private readonly Vec3 spawnPosition;
        private readonly float groundHeight;
        private readonly InputActionAsset controls;

        public DroneInstaller(DroneId droneId, Vec3 spawnPosition, float groundHeight, InputActionAsset controls)
        {
            this.droneId = droneId;
            this.spawnPosition = spawnPosition;
            this.groundHeight = groundHeight;
            this.controls = controls;
        }

        public void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(DroneConfig.Default);
            builder.RegisterInstance(FailsafeConfig.Default);
            builder.RegisterInstance(VelocityControllerConfig.Default);
            builder.RegisterInstance<ITerrain>(new FlatTerrain(groundHeight));
            builder.Register<IFlightController, VelocityFlightController>(Lifetime.Singleton)
                .WithParameter("gravity", DroneIntegrator.StandardGravity);
            builder.Register<DroneIntegrator>(Lifetime.Singleton)
                .WithParameter("gravity", DroneIntegrator.StandardGravity);
            builder.Register<IAltitudeSensor, RangefinderAltitudeSensor>(Lifetime.Singleton)
                .WithParameter("maxRange", RangefinderAltitudeSensor.DefaultMaxRange)
                .WithParameter("updateRate", RangefinderAltitudeSensor.DefaultUpdateRate);

            builder.RegisterInstance(controls);
            builder.Register<InputActionInputProvider>(Lifetime.Singleton);
            builder.Register<DisconnectableInputProvider>(Lifetime.Singleton)
                .WithParameter<IInputProvider>(resolver => resolver.Resolve<InputActionInputProvider>());
            builder.Register<LinkLossToggle>(Lifetime.Singleton);
            builder.Register<InputRouter>(Lifetime.Singleton);
            builder.Register<IFlightGate, PilotInControlGate>(Lifetime.Singleton);
            builder.Register<FlightSupervisor>(Lifetime.Singleton)
                .WithParameter("gravity", DroneIntegrator.StandardGravity);

            builder.Register<FlightLoop>(Lifetime.Singleton)
                .WithParameter("droneId", droneId)
                .WithParameter("initialState", DroneState.At(spawnPosition))
                .WithParameter<IInputProvider>(resolver => resolver.Resolve<InputRouter>());

            builder.RegisterBuildCallback(container =>
                container.Resolve<InputRouter>().Assign(LocalPilot, container.Resolve<DisconnectableInputProvider>()));
        }
    }
}
