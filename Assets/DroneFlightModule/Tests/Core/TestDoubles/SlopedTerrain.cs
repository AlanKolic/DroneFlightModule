namespace DroneFlightModule.Core.Tests
{
    /// <summary>Ground rises one metre per metre along X.</summary>
    internal sealed class SlopedTerrain : ITerrain
    {
        public float HeightAt(float x, float z)
        {
            return x;
        }
    }
}
