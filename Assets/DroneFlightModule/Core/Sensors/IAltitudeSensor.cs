namespace DroneFlightModule.Core
{
    public interface IAltitudeSensor
    {
        AltitudeReading Read(in DroneState truth, double time);
    }
}
