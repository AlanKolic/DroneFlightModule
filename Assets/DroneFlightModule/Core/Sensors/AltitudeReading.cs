namespace DroneFlightModule.Core
{
    /// <summary>
    /// A measured height above ground level. Only meaningful when <see cref="IsValid"/> is true.
    /// </summary>
    public readonly struct AltitudeReading
    {
        public static readonly AltitudeReading Invalid = new AltitudeReading(0f, false);

        public readonly float HeightAboveGround;
        public readonly bool IsValid;

        public AltitudeReading(float heightAboveGround, bool isValid)
        {
            HeightAboveGround = heightAboveGround;
            IsValid = isValid;
        }
    }
}
