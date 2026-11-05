using System;
using System.Threading;
using DroneFlightModule.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using VContainer;

namespace DroneFlightModule.Composition.Tests
{
    /// <summary>
    /// The real fleet container with a keyboard and two drones, one airborne and one on the
    /// ground, driven through the Input System test fixture.
    /// </summary>
    public abstract class FleetTestFixture : InputTestFixture
    {
        protected const float DeltaTime = 0.02f;
        protected const float GroundHeight = 1f;

        protected static readonly DroneId AirborneDrone = new DroneId("drone-1");
        protected static readonly DroneId GroundedDrone = new DroneId("drone-2");
        protected static readonly Vector3 SpawnStep = new Vector3(4f, 0f, 0f);

        protected Keyboard keyboard;
        protected InputActionAsset controls;
        protected FleetSettings settings;
        protected IObjectResolver fleet;
        protected DroneInstantiator drones;
        protected IObjectResolver airborne;
        protected IObjectResolver grounded;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            controls = new TestFleetSettings().LoadControls();
            settings = Settings(TimeSpan.Zero);
            var builder = new ContainerBuilder();
            new FleetInstaller(settings, controls, CancellationToken.None, null).Install(builder);
            fleet = builder.Build();
            drones = fleet.Resolve<DroneInstantiator>();
            drones.AddDrone(AirborneDrone, new Vector3(0f, 5f, 0f));
            drones.AddDrone(GroundedDrone, new Vector3(4f, GroundHeight, 0f));
            airborne = Scope(AirborneDrone);
            grounded = Scope(GroundedDrone);
        }

        public override void TearDown()
        {
            fleet?.Dispose();
            UnityEngine.Object.DestroyImmediate(controls);
            base.TearDown();
        }

        protected FleetSettings Settings(TimeSpan serverLatency)
        {
            return new TestFleetSettings().Create(GroundHeight, serverLatency, SpawnStep);
        }

        protected IObjectResolver Scope(DroneId id)
        {
            Assert.That(drones.TryGetScope(id, out var scope), Is.True, $"{id} should be in the fleet.");
            return scope;
        }

        protected void Tap(ButtonControl button)
        {
            Press(button);
            Release(button);
        }

        protected void AssertFlownBy(IObjectResolver drone, PilotId pilot)
        {
            Assert.That(drone.Resolve<InputRouter>().ActivePilotId, Is.EqualTo(pilot));
            Assert.That(drone.Resolve<IFlightGate>().IsFlightAllowed, Is.True);
        }

        protected void AssertFree(IObjectResolver drone)
        {
            Assert.That(drone.Resolve<InputRouter>().ActivePilotId.IsEmpty, Is.True);
            Assert.That(drone.Resolve<IFlightGate>().IsFlightAllowed, Is.False);
        }
    }
}
