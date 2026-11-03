using System;

namespace DroneFlightModule.Authorization
{
    /// <summary>
    /// A provider's answer: a grant, or a human-readable reason for the denial.
    /// </summary>
    public sealed class AuthorizationResult
    {
        private AuthorizationResult(AuthorizationGrant grant, string denialReason)
        {
            Grant = grant;
            DenialReason = denialReason;
        }

        public AuthorizationGrant Grant { get; }
        public string DenialReason { get; }
        public bool IsGranted => Grant != null;

        public static AuthorizationResult Granted(AuthorizationGrant grant)
        {
            return new AuthorizationResult(grant ?? throw new ArgumentNullException(nameof(grant)), null);
        }

        public static AuthorizationResult Denied(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("A denial needs a reason.", nameof(reason));
            }
            return new AuthorizationResult(null, reason);
        }

        public override string ToString() => IsGranted ? $"granted: {Grant}" : $"denied: {DenialReason}";
    }
}
