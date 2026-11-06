using DroneFlightModule.Authorization;
using DroneFlightModule.Pilots;

namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// Chooses which provider authorizes a pilot, so seating never has to know the providers.
    /// </summary>
    public interface IAuthorizationProviderSelector
    {
        IAuthorizationProvider For(PilotProfile pilot);
    }
}
