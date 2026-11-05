using System;
using UnityEngine;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// One drone the fleet starts with: its id and where it starts. Its view is built from the
    /// fleet's drone prefab.
    /// </summary>
    [Serializable]
    public sealed class FleetDroneEntry
    {
        [SerializeField] private string id;
        [SerializeField] private Vector3 spawnPosition;

        public FleetDroneEntry(string id, Vector3 spawnPosition)
        {
            this.id = id;
            this.spawnPosition = spawnPosition;
        }

        public string Id => id;
        public Vector3 SpawnPosition => spawnPosition;
    }
}
