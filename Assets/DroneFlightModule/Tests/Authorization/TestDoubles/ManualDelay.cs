using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DroneFlightModule.Authorization.Tests
{
    /// <summary>
    /// A delay the test completes by hand. Cancelling a wait's token cancels that wait at once.
    /// </summary>
    internal sealed class ManualDelay : IDelay
    {
        private readonly List<(TaskCompletionSource<bool> Completion, CancellationTokenRegistration Registration)> pending =
            new List<(TaskCompletionSource<bool> Completion, CancellationTokenRegistration Registration)>();

        public TimeSpan? LastDuration { get; private set; }
        public int PendingCount => pending.Count;

        public Task Wait(TimeSpan duration, CancellationToken cancellationToken)
        {
            LastDuration = duration;
            var completion = new TaskCompletionSource<bool>();
            var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
            pending.Add((completion, registration));
            return completion.Task;
        }

        public void CompleteAll()
        {
            var waits = pending.ToArray();
            pending.Clear();
            foreach (var wait in waits)
            {
                wait.Registration.Dispose();
                wait.Completion.TrySetResult(true);
            }
        }
    }
}
