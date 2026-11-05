using DroneFlightModule.Composition;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace DroneFlightModule.UI
{
    /// <summary>
    /// The dashboard's own child scope, holding the fleet dashboard and the control panel.
    /// Everything they show or drive comes from the fleet scope, its parent, which the scene names
    /// by type, so nothing outside this assembly refers to them and deleting the UI folder leaves
    /// only a missing-script warning. It adds just the event log, the readout and the panel's
    /// choices; the dashboard is bound to the fleet's Debug/ToggleDashboard action and the panel to
    /// the fleet's console.
    /// </summary>
    [RequireComponent(typeof(FleetDashboard), typeof(ControlPanel))]
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
                GetComponent<FleetDashboard>().Bind(
                    container.Resolve<FleetDashboardReadout>(),
                    container.Resolve<InputActionAsset>().FindAction("Debug/ToggleDashboard", throwIfNotFound: true));
                GetComponent<ControlPanel>().Bind(container.Resolve<IFleetConsole>(), container.Resolve<ControlPanelChoices>());
            });
        }
    }
}
