using UnityEngine;
using UnityEngine.InputSystem;

namespace DroneFlightModule.Unity
{
    /// <summary>
    /// The virtual gamepad behind the on-screen flight buttons: a layout of its own, so the input
    /// devices asset can list it as an input. It lives here rather than with the deletable UI, so
    /// that entry stays valid when the UI is gone; no device of it is then ever made.
    /// </summary>
    public sealed class ScreenPanelGamepad : Gamepad
    {
        // Unity runs only static methods at load, which is the one place a layout can register before a scene uses it.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            InputSystem.RegisterLayout<ScreenPanelGamepad>();
        }
    }
}
