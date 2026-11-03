using System.Threading;
using System.Threading.Tasks;

namespace DroneFlightModule.Authorization
{
    /// <summary>
    /// Answers one question: is this pilot allowed to fly at all? Cancelling the token ends the
    /// request with an OperationCanceledException. A new provider is a new class and one
    /// registration; nothing in flight or coordination changes.
    /// </summary>
    public interface IAuthorizationProvider
    {
        string Id { get; }

        Task<AuthorizationResult> AuthorizeAsync(AuthorizationRequest request, CancellationToken cancellationToken);
    }
}
