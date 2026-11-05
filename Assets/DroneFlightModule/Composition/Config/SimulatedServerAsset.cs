using System;
using UnityEngine;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// How long the simulated authorization server takes to answer, in seconds; the server
    /// rejects a negative latency.
    /// </summary>
    [CreateAssetMenu(menuName = "DroneFlightModule/Simulated Server", fileName = "SimulatedServer")]
    public sealed class SimulatedServerAsset : ScriptableObject
    {
        [SerializeField] private float latencySeconds = 0.5f;

        public TimeSpan ToLatency()
        {
            return TimeSpan.FromSeconds(latencySeconds);
        }
    }
}
