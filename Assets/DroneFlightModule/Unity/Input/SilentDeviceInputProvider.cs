using System;
using DroneFlightModule.Core;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace DroneFlightModule.Unity
{
    /// <summary>
    /// Reports a device as disconnected once it has sent nothing for the silence timeout, because
    /// macOS can drop a Bluetooth pad without telling the Input System. An idle pad looks the same,
    /// so any input from it reconnects it at once. A device gets the full timeout from the moment it
    /// is attached, and a timeout of zero never expires, for wired devices that report their
    /// removal.
    /// </summary>
    public sealed class SilentDeviceInputProvider : IInputProvider
    {
        private readonly ArgumentGuard guard = new ArgumentGuard();
        private readonly IInputProvider source;
        private readonly float silenceTimeout;
        private InputDevice device;
        private double attachedAt;

        public SilentDeviceInputProvider(IInputProvider source, InputDevice device, float silenceTimeout)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            this.silenceTimeout = guard.RequireNonNegative(silenceTimeout, nameof(silenceTimeout));
            Attach(device);
        }

        public bool IsConnected => source.IsConnected && !IsSilent;

        private bool IsSilent
        {
            get
            {
                if (silenceTimeout <= 0f)
                {
                    return false;
                }
                return InputState.currentTime - Math.Max(device.lastUpdateTime, attachedAt) > silenceTimeout;
            }
        }

        public void Attach(InputDevice device)
        {
            this.device = device ?? throw new ArgumentNullException(nameof(device));
            attachedAt = InputState.currentTime;
        }

        public ControlInput Read()
        {
            return source.Read();
        }
    }
}
