namespace DroneFlightModule.Core.Tests
{
    internal static class TestConfigs
    {
        public const float DeltaTime = 0.02f;

        public static readonly DroneConfig Dragless = new DroneConfig(
            mass: 1.5f,
            yawInertia: 0.05f,
            linearDrag: 0f,
            angularDrag: 0f,
            maxThrust: 30f,
            maxLateralForce: 10f,
            maxYawTorque: 0.5f);
    }
}
