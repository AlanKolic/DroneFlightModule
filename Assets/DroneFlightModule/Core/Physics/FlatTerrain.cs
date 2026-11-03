namespace DroneFlightModule.Core
{
    /// <summary>
    /// Ground at one constant height everywhere, which is all the prototype scene needs.
    /// </summary>
    public sealed class FlatTerrain : ITerrain
    {
        public FlatTerrain(float height = 0f)
        {
            Height = height;
        }

        public float Height { get; }

        public float HeightAt(float x, float z) => Height;
    }
}
