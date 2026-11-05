using System.Collections.Generic;
using UnityEngine;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Where the fleet flies: the ground height, the drones it starts with, and the step between
    /// drones added later. The defaults are three drones in a row, centred on the origin.
    /// </summary>
    [CreateAssetMenu(menuName = "DroneFlightModule/Fleet Layout", fileName = "FleetLayout")]
    public sealed class FleetLayoutAsset : ScriptableObject
    {
        [SerializeField] private float groundHeight = 0f;
        [SerializeField] private Vector3 spawnStep = new Vector3(4f, 0f, 0f);
        [SerializeField] private FleetDroneEntry[] drones =
        {
            new FleetDroneEntry("drone-1", new Vector3(-4f, 0f, 0f)),
            new FleetDroneEntry("drone-2", new Vector3(0f, 0f, 0f)),
            new FleetDroneEntry("drone-3", new Vector3(4f, 0f, 0f)),
        };

        public float GroundHeight => groundHeight;
        public Vector3 SpawnStep => spawnStep;

        public IReadOnlyList<FleetDroneEntry> Drones => drones;
    }
}
