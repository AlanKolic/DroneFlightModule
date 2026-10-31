namespace DroneFlightModule.Core
{
    /// <summary>
    /// A measured height above ground level. Only meaningful when <see cref="IsValid"/> is true.
    /// </summary>
    public readonly struct AltitudeReading
    {
        public readonly float HeightAboveGround;
        public readonly bool IsValid;
        public readonly double Timestamp;

        public AltitudeReading(float heightAboveGround, bool isValid, double timestamp)
        {
            HeightAboveGround = heightAboveGround;
            IsValid = isValid;
            Timestamp = timestamp;
        }

        public static AltitudeReading Invalid(double timestamp)
        {
            return new AltitudeReading(0f, false, timestamp);
        }
    }
}
