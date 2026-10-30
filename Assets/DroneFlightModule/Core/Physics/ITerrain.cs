namespace DroneFlightModule.Core
{
    /// <summary>
    /// The single definition of where the ground is. Physics and sensors both query it.
    /// </summary>
    public interface ITerrain
    {
        float HeightAt(float x, float z);
    }
}
