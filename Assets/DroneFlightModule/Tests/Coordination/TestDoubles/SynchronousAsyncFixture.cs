using System.Threading;
using NUnit.Framework;

namespace DroneFlightModule.Coordination.Tests
{
    /// <summary>
    /// Clears the synchronization context for each test, so await continuations run inline when a
    /// test completes a delay. Async code then finishes synchronously and tests need no waiting.
    /// </summary>
    public abstract class SynchronousAsyncFixture
    {
        private SynchronizationContext previous;

        [SetUp]
        public void ClearSynchronizationContext()
        {
            previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
        }

        [TearDown]
        public void RestoreSynchronizationContext()
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }
}
