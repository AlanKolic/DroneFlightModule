using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DroneFlightModule.Authorization;
using DroneFlightModule.Core;

namespace DroneFlightModule.Coordination.Tests
{
    /// <summary>
    /// A provider the test controls: it approves or denies everyone, answers at once or when the
    /// test says so, and revokes grants on demand. Cancelling a waiting request cancels it at once.
    /// </summary>
    internal sealed class ScriptedAuthorizationProvider : IAuthorizationProvider
    {
        private readonly GrantLedger grants;
        private readonly List<(PilotId Pilot, TaskCompletionSource<AuthorizationResult> Answer, CancellationTokenRegistration Registration)> waiting =
            new List<(PilotId Pilot, TaskCompletionSource<AuthorizationResult> Answer, CancellationTokenRegistration Registration)>();

        public ScriptedAuthorizationProvider(string id)
        {
            Id = id;
            grants = new GrantLedger(id);
        }

        public string Id { get; }
        public bool IsApproving { get; set; } = true;
        public bool AnswersAtOnce { get; set; } = true;
        public int RequestCount { get; private set; }

        public Task<AuthorizationResult> AuthorizeAsync(AuthorizationRequest request, CancellationToken cancellationToken)
        {
            RequestCount++;
            if (AnswersAtOnce)
            {
                return Task.FromResult(Answer(request.PilotId));
            }
            var answer = new TaskCompletionSource<AuthorizationResult>();
            var registration = cancellationToken.Register(() => answer.TrySetCanceled(cancellationToken));
            waiting.Add((request.PilotId, answer, registration));
            return answer.Task;
        }

        public void AnswerAll()
        {
            var requests = waiting.ToArray();
            waiting.Clear();
            foreach (var request in requests)
            {
                request.Registration.Dispose();
                if (!request.Answer.Task.IsCompleted)
                {
                    request.Answer.SetResult(Answer(request.Pilot));
                }
            }
        }

        public bool Revoke(PilotId pilot)
        {
            return grants.Revoke(pilot);
        }

        private AuthorizationResult Answer(PilotId pilot)
        {
            return IsApproving
                ? new AuthorizationResult(grants.Issue(pilot))
                : new AuthorizationResult("The test provider denies everyone.");
        }
    }
}
