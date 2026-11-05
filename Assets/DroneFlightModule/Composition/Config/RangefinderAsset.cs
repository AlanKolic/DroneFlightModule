using DroneFlightModule.Core;
using UnityEngine;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Editable range and rate of the rangefinder, mapped to a <see cref="RangefinderConfig"/>,
    /// which validates them. Defaults match <see cref="RangefinderConfig.Default"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "DroneFlightModule/Rangefinder", fileName = "Rangefinder")]
    public sealed class RangefinderAsset : ScriptableObject
    {
        [SerializeField] private float maxRange = 10f;
        [SerializeField] private float updateRate = 20f;

        public RangefinderConfig ToConfig()
        {
            return new RangefinderConfig(maxRange, updateRate);
        }
    }
}
