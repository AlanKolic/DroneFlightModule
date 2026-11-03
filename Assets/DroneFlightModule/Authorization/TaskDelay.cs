using System;
using System.Threading;
using System.Threading.Tasks;

namespace DroneFlightModule.Authorization
{
    /// <summary>
    /// Real waiting with <see cref="Task.Delay(TimeSpan, CancellationToken)"/>.
    /// </summary>
    public sealed class TaskDelay : IDelay
    {
        public Task Wait(TimeSpan duration, CancellationToken cancellationToken)
        {
            return Task.Delay(duration, cancellationToken);
        }
    }
}
