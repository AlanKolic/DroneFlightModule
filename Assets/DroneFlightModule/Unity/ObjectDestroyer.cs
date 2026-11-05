using UnityEngine;

namespace DroneFlightModule.Unity
{
    /// <summary>
    /// Destroys a Unity object in play mode and in edit mode, which can't defer destruction, so
    /// code that runs in both can stay the same.
    /// </summary>
    public sealed class ObjectDestroyer
    {
        public void Destroy(Object target)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
