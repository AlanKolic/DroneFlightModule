using System;
using System.Threading;
using System.Threading.Tasks;

namespace DroneFlightModule.Authorization.Tests
{
    /// <summary>
    /// A delay that finishes only when the test completes it, even after cancellation, like a real
    /// delay that completes on another thread just as the provider is disposed.
    /// </summary>
    internal sealed class CancellationIgnoringDelay : IDelay
    {
        private readonly TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();

        public Task Wait(TimeSpan duration, CancellationToken cancellationToken)
        {
            return completion.Task;
        }

        public void Complete()
        {
            completion.SetResult(true);
        }
    }
}
