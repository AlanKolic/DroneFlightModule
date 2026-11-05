using System;
using System.Collections.Generic;
using DroneFlightModule.Coordination;
using DroneFlightModule.Core;
using DroneFlightModule.Unity;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Adds drones to the running fleet and removes them. Each drone gets its own child scope of
    /// the fleet's container, ticked every fixed update, and joins the fleet as that scope is
    /// built; a view is built from the drone prefab, when there is one, and bound, and its HUD box
    /// added. A drone added without an id goes one spawn step past the last drone, with the first
    /// free drone-N id. Each drone's loop starts at the fleet's simulation time, and it starts
    /// grounded if it spawns on the terrain. Removing a drone has the coordinator remove it, which
    /// releases it first, then disposes its scope and destroys its view and HUD box; the next drone
    /// added goes one step past the last drone it added. Disposing disposes every drone's scope
    /// and destroys their views.
    /// </summary>
    public sealed class DroneInstantiator : IDisposable
    {
        private readonly IObjectResolver fleet;
        private readonly DroneRegistry registry;
        private readonly FlightCoordinator coordinator;
        private readonly ISimulationClock clock;
        private readonly DroneView prefab;
        private readonly FlightDebugHud hud;
        private readonly ITerrain terrain;
        private readonly Vector3 spawnStep;
        private readonly ObjectDestroyer destroyer = new ObjectDestroyer();
        private readonly Dictionary<DroneId, Member> members = new Dictionary<DroneId, Member>();
        private Vector3 nextSpawn;

        public DroneInstantiator(
            IObjectResolver fleet,
            DroneRegistry registry,
            FlightCoordinator coordinator,
            ISimulationClock clock,
            DroneView prefab,
            FlightDebugHud hud,
            ITerrain terrain,
            Vector3 spawnStep)
        {
            this.fleet = fleet ?? throw new ArgumentNullException(nameof(fleet));
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.prefab = prefab;
            this.hud = hud;
            this.terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            this.spawnStep = spawnStep;
            nextSpawn = FirstSpawn;
        }

        private Vector3 FirstSpawn => new Vector3(0f, terrain.HeightAt(0f, 0f), 0f);

        public DroneId AddDrone()
        {
            var id = NextFreeId();
            AddDrone(id, nextSpawn);
            return id;
        }

        public void AddDrone(DroneId id, Vector3 spawnPosition)
        {
            if (registry.TryGet(id, out _))
            {
                throw new ArgumentException($"Drone {id} is already in the fleet.", nameof(id));
            }
            var spawn = spawnPosition.ToVec3();
            bool isGrounded = spawn.Y <= terrain.HeightAt(spawn.X, spawn.Z);
            var initialState = new DroneState(spawn, Vec3.Zero, 0f, 0f, isGrounded);
            var scope = fleet.CreateScope(builder =>
            {
                new DroneInstaller(id, initialState, clock.Time).Install(builder);
                builder.RegisterEntryPoint<FlightLoopRunner>();
            });
            var view = BuildView(id, spawnPosition);
            members.Add(id, new Member(scope, view, spawnPosition));

            var loop = scope.Resolve<FlightLoop>();
            if (view != null)
            {
                view.Bind(loop);
            }
            if (hud != null)
            {
                hud.AddDrone(loop, scope.Resolve<InputRouter>());
            }
            nextSpawn = spawnPosition + spawnStep;
        }

        public bool RemoveDrone(DroneId id)
        {
            if (!members.TryGetValue(id, out var member))
            {
                return false;
            }
            members.Remove(id);
            coordinator.RemoveDrone(id);
            if (hud != null)
            {
                hud.RemoveDrone(member.Scope.Resolve<FlightLoop>());
            }
            DestroyView(member.View);
            member.Scope.Dispose();

            nextSpawn = FirstSpawn;
            foreach (var drone in registry.Drones)
            {
                if (members.TryGetValue(drone.Id, out var remaining))
                {
                    nextSpawn = remaining.SpawnPosition + spawnStep;
                }
            }
            return true;
        }

        public bool RemoveNewestDrone()
        {
            var drones = registry.Drones;
            return drones.Count > 0 && RemoveDrone(drones[drones.Count - 1].Id);
        }

        // The drone's own container, for tests that need its parts.
        internal bool TryGetScope(DroneId id, out IObjectResolver scope)
        {
            bool isMember = members.TryGetValue(id, out var member);
            scope = isMember ? member.Scope : null;
            return isMember;
        }

        public bool TryGetView(DroneId id, out DroneView view)
        {
            view = members.TryGetValue(id, out var member) ? member.View : null;
            return view != null;
        }

        public void Dispose()
        {
            foreach (var member in members.Values)
            {
                member.Scope.Dispose();
                DestroyView(member.View);
            }
            members.Clear();
        }

        // The first drone-N not taken, so ids stay readable however the fleet was configured.
        private DroneId NextFreeId()
        {
            int number = registry.Drones.Count + 1;
            while (registry.TryGet(new DroneId($"drone-{number}"), out _))
            {
                number++;
            }
            return new DroneId($"drone-{number}");
        }

        private DroneView BuildView(DroneId id, Vector3 spawnPosition)
        {
            if (prefab == null)
            {
                return null;
            }
            var view = UnityEngine.Object.Instantiate(prefab, spawnPosition, Quaternion.identity);
            view.name = id.ToString();
            return view;
        }

        private void DestroyView(DroneView view)
        {
            if (view == null)
            {
                return;
            }
            destroyer.Destroy(view.gameObject);
        }

        private sealed class Member
        {
            public Member(IScopedObjectResolver scope, DroneView view, Vector3 spawnPosition)
            {
                Scope = scope;
                View = view;
                SpawnPosition = spawnPosition;
            }

            public IScopedObjectResolver Scope { get; }
            public DroneView View { get; }
            public Vector3 SpawnPosition { get; }
        }
    }
}
