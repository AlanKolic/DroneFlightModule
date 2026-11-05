using System;
using System.IO;
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
        private const string ControlsPath = "Assets/DroneFlightModule/Unity/Input/DroneControls.inputactions";
        private const float GroundHeight = 1f;

        private static readonly Vector3 SpawnStep = new Vector3(4f, 0f, 0f);

        private InputActionAsset controls;
        private IObjectResolver fleet;
        private DroneView prefab;
        private DroneInstantiator drones;

        [SetUp]
        public void SetUp()
        {
            controls = InputActionAsset.FromJson(File.ReadAllText(ControlsPath));
            var builder = new ContainerBuilder();
            new FleetInstaller(new TestFleetSettings().Create(GroundHeight, TimeSpan.Zero, SpawnStep), controls, CancellationToken.None, null, null).Install(builder);
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
                fleet, fleet.Resolve<DroneRegistry>(), fleet.Resolve<FlightCoordinator>(), fleet.Resolve<ISimulationClock>(), dronePrefab, null, GroundHeight, SpawnStep);
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

            Assert.Throws<ArgumentNullException>(() => new DroneInstantiator(null, registry, coordinator, clock, prefab, null, 0f, SpawnStep));
            Assert.Throws<ArgumentNullException>(() => new DroneInstantiator(fleet, null, coordinator, clock, prefab, null, 0f, SpawnStep));
            Assert.Throws<ArgumentNullException>(() => new DroneInstantiator(fleet, registry, null, clock, prefab, null, 0f, SpawnStep));
            Assert.Throws<ArgumentNullException>(() => new DroneInstantiator(fleet, registry, coordinator, null, prefab, null, 0f, SpawnStep));
        }

        [Test]
        public void AConfiguredDrone_JoinsTheFleet_AndStartsGroundedOnlyOnTheGround()
        {
            var high = new DroneId("high");
            var low = new DroneId("low");

            drones.AddDrone(high, new Vector3(0f, 5f, 0f), null);
            drones.AddDrone(low, new Vector3(4f, GroundHeight, 0f), null);

            Assert.That(Registry.Drones[0].Id, Is.EqualTo(high));
            Assert.That(Registry.Drones[1].Id, Is.EqualTo(low));
            Assert.That(Loop(high).State.IsGrounded, Is.False);
            Assert.That(Loop(low).State.IsGrounded, Is.True);
        }

        [Test]
        public void ADroneFromThePrefab_GetsTheNextIdAndSpawnsOneStepPastTheLast()
        {
            drones.AddDrone(new DroneId("drone-1"), new Vector3(0f, GroundHeight, 0f), null);

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
            drones.AddDrone(id, Vector3.zero, null);
            int views = UnityEngine.Object.FindObjectsByType<DroneView>(FindObjectsSortMode.None).Length;

            Assert.Throws<ArgumentException>(() => drones.AddDrone(id, Vector3.zero));

            Assert.That(UnityEngine.Object.FindObjectsByType<DroneView>(FindObjectsSortMode.None), Has.Length.EqualTo(views));
        }

        [Test]
        public void NewIds_SkipTakenOnes()
        {
            drones.AddDrone(new DroneId("drone-2"), new Vector3(0f, GroundHeight, 0f), null);

            Assert.That(drones.AddDrone(), Is.EqualTo(new DroneId("drone-3")));
        }

        [Test]
        public void WithoutAPrefab_AddingFromItThrows()
        {
            var withoutPrefab = Instantiator(null);

            Assert.Throws<InvalidOperationException>(() => withoutPrefab.AddDrone());
            Assert.Throws<InvalidOperationException>(() => withoutPrefab.AddDrone(new DroneId("scout"), Vector3.zero));
            Assert.That(Registry.Drones, Is.Empty);
        }

        [Test]
        public void ATakenId_IsRefusedBeforeAnythingIsBuilt()
        {
            var id = new DroneId("drone-1");
            drones.AddDrone(id, Vector3.zero, null);

            Assert.Throws<ArgumentException>(() => drones.AddDrone(id, Vector3.zero, null));
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
        public void Dispose_DestroysOnlyTheViewsItBuilt()
        {
            var given = new GameObject("Given", typeof(DroneView)).GetComponent<DroneView>();
            drones.AddDrone(new DroneId("given"), Vector3.zero, given);
            var built = drones.AddDrone();
            drones.TryGetView(built, out var builtView);

            drones.Dispose();

            Assert.That(builtView == null, Is.True);
            Assert.That(given == null, Is.False);
            UnityEngine.Object.DestroyImmediate(given.gameObject);
        }
    }
}
