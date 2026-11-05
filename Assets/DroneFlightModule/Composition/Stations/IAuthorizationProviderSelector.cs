using DroneFlightModule.Authorization;
using DroneFlightModule.Pilots;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// Chooses which provider authorizes a pilot, so a station never has to know the providers.
    /// </summary>
    public interface IAuthorizationProviderSelector
    {
        IAuthorizationProvider For(PilotProfile pilot);
    }
}
