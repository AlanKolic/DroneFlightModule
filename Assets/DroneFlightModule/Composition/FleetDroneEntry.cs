using System;
using DroneFlightModule.Unity;
using UnityEngine;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// One drone of the fleet scene: its id, where it starts and the view that shows it.
    /// </summary>
    [Serializable]
    public sealed class FleetDroneEntry
    {
        [SerializeField] private string id = "drone-1";
        [SerializeField] private Vector3 spawnPosition = Vector3.zero;
        [SerializeField] private DroneView view;

        public string Id => id;
        public Vector3 SpawnPosition => spawnPosition;
        public DroneView View => view;
    }
}
