using System;

namespace DroneFlightModule.Authorization
{
    /// <summary>
    /// A provider's answer: a grant, or a human-readable reason for the denial.
    /// </summary>
    public sealed class AuthorizationResult
    {
        public AuthorizationResult(AuthorizationGrant grant)
        {
            Grant = grant ?? throw new ArgumentNullException(nameof(grant));
        }

        public AuthorizationResult(string denialReason)
        {
            if (string.IsNullOrWhiteSpace(denialReason))
            {
                throw new ArgumentException("A denial needs a reason.", nameof(denialReason));
            }
            DenialReason = denialReason;
        }

        public AuthorizationGrant Grant { get; }
        public string DenialReason { get; }
        public bool IsGranted => Grant != null;

        public override string ToString() => IsGranted ? $"granted: {Grant}" : $"denied: {DenialReason}";
    }
}
