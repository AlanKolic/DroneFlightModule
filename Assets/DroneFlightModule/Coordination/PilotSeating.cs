using System;
using System.Collections.Generic;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;

namespace DroneFlightModule.Coordination
{
    /// <summary>
    /// Who sits at which desk, and the only place that changes it, so a pilot is never at two desks.
    /// A pilot sitting down gets a new session; leaving ends it, which releases their drone. A pilot
    /// who sits down at another desk moves there instead: their session goes with them and keeps
    /// its drone and grant, and the desk they left is empty. Only a pending request is lost,
    /// because its outcome would report to that desk.
    /// </summary>
    public sealed class PilotSeating
    {
        private readonly FlightCoordinator coordinator;
        private readonly IPilotRegistry pilots;
        private readonly IAuthorizationProviderSelector providers;
        private readonly Dictionary<PilotId, IPilotDesk> seats = new Dictionary<PilotId, IPilotDesk>();

        public PilotSeating(FlightCoordinator coordinator, IPilotRegistry pilots, IAuthorizationProviderSelector providers)
        {
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            this.pilots = pilots ?? throw new ArgumentNullException(nameof(pilots));
            this.providers = providers ?? throw new ArgumentNullException(nameof(providers));
        }

        // The guest if nobody has them, otherwise nobody.
        public PilotId StartPilot()
        {
            return seats.ContainsKey(PilotProfile.Guest.Id) ? default : PilotProfile.Guest.Id;
        }

        public string SeatOf(PilotId pilot)
        {
            return seats.TryGetValue(pilot, out var desk) ? desk.Name : string.Empty;
        }

        public void Seat(IPilotDesk desk, PilotId pilot)
        {
            if (!pilots.TryGet(pilot, out var profile))
            {
                throw new ArgumentException($"{pilot} is not in the pilot registry.", nameof(pilot));
            }
            if (seats.TryGetValue(pilot, out var current) && current == desk)
            {
                return;
            }
            Vacate(desk);
            if (current != null)
            {
                Move(current, desk);
                return;
            }
            var session = new PilotSession(pilot, providers.For(profile), desk.Input);
            seats.Add(pilot, desk);
            desk.Occupy(session);
            desk.Report($"{pilot} sits here. Request a drone.");
        }

        public void Vacate(IPilotDesk desk)
        {
            var session = desk.Session;
            if (session == null)
            {
                return;
            }
            seats.Remove(session.PilotId);
            coordinator.EndSession(session);
            desk.Occupy(null);
            desk.Report($"{session.PilotId} left the desk.");
        }

        // Each free pilot in the registry's order, then empty, then round again. A pilot at another
        // desk is skipped: moving them is a deliberate choice, and a key press must not end their flight.
        public void SeatNext(IPilotDesk desk)
        {
            bool isPastSeatedPilot = desk.Session == null;
            foreach (var profile in pilots.Profiles)
            {
                if (isPastSeatedPilot && !seats.ContainsKey(profile.Id))
                {
                    Seat(desk, profile.Id);
                    return;
                }
                if (desk.Session != null && profile.Id == desk.Session.PilotId)
                {
                    isPastSeatedPilot = true;
                }
            }
            Vacate(desk);
        }

        private void Move(IPilotDesk from, IPilotDesk to)
        {
            var session = from.Session;
            string cancelled = coordinator.CancelRequest(session) ? $" {session.PilotId}'s request was cancelled." : "";
            from.Occupy(null);
            from.Report($"{session.PilotId} moved to {to.Name}.");
            session.MoveTo(to.Input);
            seats[session.PilotId] = to;
            to.Occupy(session);
            coordinator.Reroute(session);
            to.Report($"{session.PilotId} moved here.{cancelled}");
        }
    }
}
