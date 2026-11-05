using DroneFlightModule.Core;

namespace DroneFlightModule.Authorization
{
    /// <summary>
    /// A provider that can take back the grant it issued a pilot, such as a server whose operator
    /// grounds them. Revoking returns false when the pilot holds no live grant from it.
    /// </summary>
    public interface IRevokingAuthorizationProvider : IAuthorizationProvider
    {
        bool Revoke(PilotId pilot);
    }
}
