using System;
using System.Threading;
using DroneFlightModule.Coordination;
using DroneFlightModule.Core;
using DroneFlightModule.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace DroneFlightModule.Composition.Tests
{
    public class DroneInstantiatorTests
    {
        private sealed class SteppedTerrain : ITerrain
        {
            public float HeightAt(float x, float z) => x > 0f ? 3f : 0f;
        }

        private const float GroundHeight = 1f;

        private static readonly Vector3 SpawnStep = new Vector3(4f, 0f, 0f);

        private InputActionAsset controls;
        private IObjectResolver fleet;
        private DroneView prefab;
        private DroneInstantiator drones;

        [SetUp]
        public void SetUp()
        {
            controls = new TestFleetSettings().LoadControls();
            var builder = new ContainerBuilder();
            new FleetInstaller(new TestFleetSettings().Create(GroundHeight, TimeSpan.Zero, SpawnStep), controls, CancellationToken.None, null).Install(builder);
            fleet = builder.Build();
            prefab = new GameObject("Drone Prefab", typeof(DroneView)).GetComponent<DroneView>();
            drones = Instantiator(prefab);
        }

        [TearDown]
        public void TearDown()
        {
            drones.Dispose();
            fleet.Dispose();
            UnityEngine.Object.DestroyImmediate(prefab.gameObject);
            UnityEngine.Object.DestroyImmediate(controls);
        }

        private DroneInstantiator Instantiator(DroneView dronePrefab)
        {
            return new DroneInstantiator(
                fleet, fleet.Resolve<DroneRegistry>(), fleet.Resolve<FlightCoordinator>(), fleet.Resolve<ISimulationClock>(), dronePrefab, fleet.Resolve<ITerrain>(), SpawnStep);
        }

        private DroneRegistry Registry => fleet.Resolve<DroneRegistry>();

        private FlightLoop Loop(DroneId id)
        {
            Assert.That(drones.TryGetScope(id, out var scope), Is.True, $"{id} should be in the fleet.");
            return scope.Resolve<FlightLoop>();
        }

        [Test]
        public void Constructor_RequiresTheFleet()
        {
            var registry = Registry;
            var coordinator = fleet.Resolve<FlightCoordinator>();
            var clock = fleet.Resolve<ISimulationClock>();
            var terrain = fleet.Resolve<ITerrain>();

            Assert.Throws<ArgumentNullException>(() => new DroneInstantiator(null, registry, coordinator, clock, prefab, terrain, SpawnStep));
            Assert.Throws<ArgumentNullException>(() => new DroneInstantiator(fleet, null, coordinator, clock, prefab, terrain, SpawnStep));
            Assert.Throws<ArgumentNullException>(() => new DroneInstantiator(fleet, registry, null, clock, prefab, terrain, SpawnStep));
            Assert.Throws<ArgumentNullException>(() => new DroneInstantiator(fleet, registry, coordinator, null, prefab, terrain, SpawnStep));
            Assert.Throws<ArgumentNullException>(() => new DroneInstantiator(fleet, registry, coordinator, clock, prefab, null, SpawnStep));
        }

        [Test]
        public void AConfiguredDrone_JoinsTheFleet_AndStartsGroundedOnlyOnTheGround()
        {
            var high = new DroneId("high");
            var low = new DroneId("low");

            drones.AddDrone(high, new Vector3(0f, 5f, 0f));
            drones.AddDrone(low, new Vector3(4f, GroundHeight, 0f));

            Assert.That(Registry.Drones[0].Id, Is.EqualTo(high));
            Assert.That(Registry.Drones[1].Id, Is.EqualTo(low));
            Assert.That(Loop(high).State.IsGrounded, Is.False);
            Assert.That(Loop(low).State.IsGrounded, Is.True);
        }

        [Test]
        public void ADroneFromThePrefab_GetsTheNextIdAndSpawnsOneStepPastTheLast()
        {
            drones.AddDrone(new DroneId("drone-1"), new Vector3(0f, GroundHeight, 0f));

            var id = drones.AddDrone();

            Assert.That(id, Is.EqualTo(new DroneId("drone-2")));
            Assert.That(drones.TryGetView(id, out var view), Is.True);
            Assert.That(view.name, Is.EqualTo("drone-2"));
            Assert.That(view.transform.position, Is.EqualTo(new Vector3(4f, GroundHeight, 0f)));
            Assert.That(Loop(id).State.Position, Is.EqualTo(new Vec3(4f, GroundHeight, 0f)));
        }

        [Test]
        public void ADroneWithAGivenId_GetsAViewFromThePrefabWhereItSpawns()
        {
            var id = new DroneId("scout");

            drones.AddDrone(id, new Vector3(-4f, GroundHeight, 2f));

            Assert.That(drones.TryGetView(id, out var view), Is.True);
            Assert.That(view.name, Is.EqualTo("scout"));
            Assert.That(view.transform.position, Is.EqualTo(new Vector3(-4f, GroundHeight, 2f)));
            Assert.That(Loop(id).State.Position, Is.EqualTo(new Vec3(-4f, GroundHeight, 2f)));
        }

        [Test]
        public void ATakenId_BuildsNoViewFromThePrefab()
        {
            var id = new DroneId("drone-1");
            drones.AddDrone(id, Vector3.zero);
            int views = UnityEngine.Object.FindObjectsByType<DroneView>(FindObjectsSortMode.None).Length;

            Assert.Throws<ArgumentException>(() => drones.AddDrone(id, Vector3.zero));

            Assert.That(UnityEngine.Object.FindObjectsByType<DroneView>(FindObjectsSortMode.None), Has.Length.EqualTo(views));
        }

        [Test]
        public void NewIds_SkipTakenOnes()
        {
            drones.AddDrone(new DroneId("drone-2"), new Vector3(0f, GroundHeight, 0f));

            Assert.That(drones.AddDrone(), Is.EqualTo(new DroneId("drone-1")));
            Assert.That(drones.AddDrone(), Is.EqualTo(new DroneId("drone-3")));
        }

        [Test]
        public void WithoutAPrefab_ADroneHasNoView()
        {
            var withoutPrefab = Instantiator(null);

            var id = withoutPrefab.AddDrone();

            Assert.That(Registry.TryGet(id, out _), Is.True);
            Assert.That(withoutPrefab.TryGetView(id, out _), Is.False);
            withoutPrefab.Dispose();
        }

        [Test]
        public void GroundedAtSpawn_ComesFromTheTerrainUnderTheDrone()
        {
            var hill = new SteppedTerrain();
            var onHill = new DroneInstantiator(
                fleet, Registry, fleet.Resolve<FlightCoordinator>(), fleet.Resolve<ISimulationClock>(), null, hill, SpawnStep);
            var hilltop = new DroneId("hilltop");
            var valley = new DroneId("valley");

            onHill.AddDrone(hilltop, new Vector3(1f, 3f, 0f));
            onHill.AddDrone(valley, new Vector3(-1f, 3f, 0f));

            onHill.TryGetScope(hilltop, out var hilltopScope);
            onHill.TryGetScope(valley, out var valleyScope);
            Assert.That(hilltopScope.Resolve<FlightLoop>().State.IsGrounded, Is.True);
            Assert.That(valleyScope.Resolve<FlightLoop>().State.IsGrounded, Is.False);
            onHill.Dispose();
        }

        [Test]
        public void ATakenId_IsRefusedBeforeAnythingIsBuilt()
        {
            var id = new DroneId("drone-1");
            drones.AddDrone(id, Vector3.zero);

            Assert.Throws<ArgumentException>(() => drones.AddDrone(id, Vector3.zero));
            Assert.That(Registry.Drones, Has.Count.EqualTo(1));
        }

        [Test]
        public void ADroneAddedLater_StartsAtTheFleetsTime()
        {
            fleet.Resolve<SimulationClock>().Advance(2.5f);

            var id = drones.AddDrone();

            Assert.That(Loop(id).Time, Is.EqualTo(2.5).Within(1e-6));
        }

        [Test]
        public void RemovingADrone_WorksWhenAnotherDroneJoinedTheRegistryDirectly()
        {
            var id = drones.AddDrone();
            var other = Instantiator(prefab);
            var stranger = other.AddDrone();

            Assert.DoesNotThrow(() => drones.RemoveDrone(id));
            Assert.That(Registry.TryGet(stranger, out _), Is.True);
            other.Dispose();
        }

        [Test]
        public void RemoveDrone_TakesItOutAndDestroysItsView()
        {
            var id = drones.AddDrone();
            drones.TryGetView(id, out var view);

            Assert.That(drones.RemoveDrone(id), Is.True);

            Assert.That(view == null, Is.True, "The view should be destroyed.");
            Assert.That(Registry.TryGet(id, out _), Is.False);
            Assert.That(drones.TryGetScope(id, out _), Is.False);
            Assert.That(drones.RemoveDrone(id), Is.False);
        }

        [Test]
        public void TheNextDrone_TakesTheRemovedDronesSpot()
        {
            drones.AddDrone();
            drones.AddDrone();

            Assert.That(drones.RemoveNewestDrone(), Is.True);
            var id = drones.AddDrone();

            Assert.That(id, Is.EqualTo(new DroneId("drone-2")));
            drones.TryGetView(id, out var view);
            Assert.That(view.transform.position, Is.EqualTo(new Vector3(4f, GroundHeight, 0f)));
        }

        [Test]
        public void RemovingEveryDrone_StartsTheRowAgain()
        {
            drones.AddDrone();
            drones.RemoveNewestDrone();

            Assert.That(drones.RemoveNewestDrone(), Is.False);
            var id = drones.AddDrone();
            drones.TryGetView(id, out var view);
            Assert.That(view.transform.position, Is.EqualTo(new Vector3(0f, GroundHeight, 0f)));
        }

        [Test]
        public void Dispose_DestroysTheViews()
        {
            var id = drones.AddDrone();
            drones.TryGetView(id, out var view);

            drones.Dispose();

            Assert.That(view == null, Is.True);
        }
    }
}
