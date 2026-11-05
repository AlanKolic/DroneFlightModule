using DroneFlightModule.Core;
using UnityEngine;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Editable range and rate of the rangefinder, mapped to a <see cref="RangefinderConfig"/>,
    /// which validates them. Defaults come from <see cref="RangefinderConfig.Default"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "DroneFlightModule/Rangefinder", fileName = "Rangefinder")]
    public sealed class RangefinderAsset : ScriptableObject
    {
        [SerializeField] private float maxRange = RangefinderConfig.Default.MaxRange;
        [SerializeField] private float updateRate = RangefinderConfig.Default.UpdateRate;

        public RangefinderConfig ToConfig()
        {
            return new RangefinderConfig(maxRange, updateRate);
        }
    }
}
