using DroneFlightModule.Composition;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace DroneFlightModule.UI
{
    /// <summary>
    /// The dashboard's own child scope, holding the fleet dashboard, the control panel, the
    /// on-screen flight buttons and the debug HUD.
    /// Everything they show or drive comes from the fleet scope, its parent, which the scene names
    /// by type, so nothing outside this assembly refers to them and deleting the UI folder leaves
    /// only a missing-script warning. It adds just the event log, the readout and the panel's
    /// choices; the dashboard is bound to the fleet's Debug/ToggleDashboard action and the panel to
    /// the fleet's console.
    /// </summary>
    [RequireComponent(typeof(FleetDashboard), typeof(ControlPanel), typeof(ScreenFlightPanel))]
    [RequireComponent(typeof(FlightDebugHud))]
    public sealed class DashboardLifetimeScope : LifetimeScope
    {
        [SerializeField] private int eventCapacity = 50;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<FleetEventLog>(Lifetime.Singleton)
                .WithParameter("capacity", eventCapacity);
            builder.Register<FleetDashboardReadout>(Lifetime.Singleton);
            builder.Register<ControlPanelChoices>(Lifetime.Singleton);
            builder.RegisterBuildCallback(container =>
            {
                var console = container.Resolve<IFleetConsole>();
                BindHud(container.Resolve<InputDeviceStations>(), container.Resolve<InputActionAsset>());
                var panel = GetComponent<ControlPanel>();
                panel.Bind(console, container.Resolve<ControlPanelChoices>());
                GetComponent<ScreenFlightPanel>().Bind(console);
                GetComponent<FleetDashboard>().Bind(
                    container.Resolve<FleetDashboardReadout>(),
                    container.Resolve<InputActionAsset>().FindAction("Debug/ToggleDashboard", throwIfNotFound: true),
                    panel);
            });
        }

        private void BindHud(InputDeviceStations stations, InputActionAsset controls)
        {
            var hud = GetComponent<FlightDebugHud>();
            hud.Bind(controls);
            foreach (var station in stations.Stations)
            {
                hud.AddStation(station);
            }
            stations.StationAdded += hud.AddStation;
            stations.StationRemoved += hud.RemoveStation;
        }
    }
}
