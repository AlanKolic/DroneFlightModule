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
    /// Adds drones to the running fleet and removes them. Each drone gets its own child scope, ticked
    /// every fixed update, and a view from the prefab when there is one. A drone added without an id
    /// goes one spawn step past the last, with the first free drone-N id. Removing one has the
    /// coordinator release it first. Disposing disposes every scope and destroys every view.
    /// </summary>
    public sealed class DroneInstantiator : IDisposable
    {
        private readonly IObjectResolver fleet;
        private readonly DroneRegistry registry;
        private readonly FlightCoordinator coordinator;
        private readonly ISimulationClock clock;
        private readonly DroneView prefab;
        private readonly ITerrain terrain;
        private readonly Vector3 spawnStep;
        private readonly ObjectDestroyer destroyer = new ObjectDestroyer();
        private readonly Dictionary<DroneId, Member> members = new Dictionary<DroneId, Member>();

        public DroneInstantiator(
            IObjectResolver fleet,
            DroneRegistry registry,
            FlightCoordinator coordinator,
            ISimulationClock clock,
            DroneView prefab,
            ITerrain terrain,
            Vector3 spawnStep)
        {
            this.fleet = fleet ?? throw new ArgumentNullException(nameof(fleet));
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.prefab = prefab;
            this.terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            this.spawnStep = spawnStep;
        }

        public DroneId AddDrone()
        {
            var id = NextFreeId();
            AddDrone(id, NextSpawn());
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
        }

        public bool RemoveDrone(DroneId id)
        {
            if (!members.TryGetValue(id, out var member))
            {
                return false;
            }
            members.Remove(id);
            coordinator.RemoveDrone(id);
            DestroyView(member.View);
            member.Scope.Dispose();
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

        // One step past the newest drone added here, or at the origin when there is none.
        private Vector3 NextSpawn()
        {
            var fleetDrones = registry.Drones;
            for (int i = fleetDrones.Count - 1; i >= 0; i--)
            {
                if (members.TryGetValue(fleetDrones[i].Id, out var newest))
                {
                    return newest.SpawnPosition + spawnStep;
                }
            }
            return new Vector3(0f, terrain.HeightAt(0f, 0f), 0f);
        }

        // The first drone-N not taken, so ids stay readable however the fleet was configured.
        private DroneId NextFreeId()
        {
            int number = 1;
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

        // What the instantiator keeps for one drone it added.
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
