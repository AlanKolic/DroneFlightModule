using DroneFlightModule.Core;
using DroneFlightModule.Unity;
using VContainer;
using VContainer.Unity;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Registers one drone's Core flight objects with plain constructors. Kept apart from the
    /// scope so tests can build the container without a scene. Until the flight coordinator
    /// exists, the local keyboard is assigned to the drone directly.
    /// </summary>
    public sealed class DroneInstaller : IInstaller
    {
        public static readonly PilotId LocalKeyboardPilot = new PilotId("local-keyboard");

        private readonly DroneId droneId;
        private readonly Vec3 spawnPosition;
        private readonly float groundHeight;

        public DroneInstaller(DroneId droneId, Vec3 spawnPosition, float groundHeight)
        {
            this.droneId = droneId;
            this.spawnPosition = spawnPosition;
            this.groundHeight = groundHeight;
        }

        public void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(DroneConfig.Default);
            builder.RegisterInstance<ITerrain>(new FlatTerrain(groundHeight));
            builder.Register<IFlightController, ForceFlightController>(Lifetime.Singleton);
            builder.Register<DroneIntegrator>(Lifetime.Singleton)
                .WithParameter("gravity", DroneIntegrator.StandardGravity);
            builder.Register<IAltitudeSensor, RangefinderAltitudeSensor>(Lifetime.Singleton)
                .WithParameter("maxRange", RangefinderAltitudeSensor.DefaultMaxRange)
                .WithParameter("updateRate", RangefinderAltitudeSensor.DefaultUpdateRate);
            builder.Register<KeyboardInputProvider>(Lifetime.Singleton);
            
            builder.Register(resolver =>
                {
                    var router = new InputRouter();
                    router.Assign(LocalKeyboardPilot, resolver.Resolve<KeyboardInputProvider>());
                    return router;
                },
                Lifetime.Singleton);

            builder.Register(
                resolver => new FlightLoop(
                    droneId,
                    resolver.Resolve<InputRouter>(),
                    resolver.Resolve<IFlightController>(),
                    resolver.Resolve<DroneIntegrator>(),
                    resolver.Resolve<IAltitudeSensor>(),
                    DroneState.At(spawnPosition)),
                Lifetime.Singleton);
        }
    }
}
