using System;
using DroneFlightModule.Authorization;
using DroneFlightModule.Pilots;

namespace DroneFlightModule.Composition
{
    /// <summary>
    /// The guest goes through local guest mode, and everyone else signs in through the server.
    /// </summary>
    public sealed class GuestOrServerSelector : IAuthorizationProviderSelector
    {
        private readonly LocalGuestAuthorizationProvider guest;
        private readonly IRevokingAuthorizationProvider server;

        public GuestOrServerSelector(LocalGuestAuthorizationProvider guest, IRevokingAuthorizationProvider server)
        {
            this.guest = guest ?? throw new ArgumentNullException(nameof(guest));
            this.server = server ?? throw new ArgumentNullException(nameof(server));
        }

        public IAuthorizationProvider For(PilotProfile pilot)
        {
            return pilot.IsGuest ? (IAuthorizationProvider)guest : server;
        }
    }
}
