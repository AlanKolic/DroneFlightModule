using System;
using System.Threading;
using System.Threading.Tasks;

namespace DroneFlightModule.Authorization
{
    /// <summary>
    /// Waiting, made injectable so tests control simulated latency instead of sleeping.
    /// </summary>
    public interface IDelay
    {
        Task Wait(TimeSpan duration, CancellationToken cancellationToken);
    }
}
