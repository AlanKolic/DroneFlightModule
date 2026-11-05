using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// Measures height above the terrain directly below the drone, with a max range and a fixed
    /// update rate. Between samples it returns the last reading. No noise.
    /// </summary>
    public sealed class RangefinderAltitudeSensor : IAltitudeSensor
    {
        private readonly ITerrain terrain;
        private readonly float maxRange;
        private readonly SampleSchedule schedule;
        private AltitudeReading last;

        public RangefinderAltitudeSensor(ITerrain terrain, RangefinderConfig config)
        {
            this.terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            maxRange = config.MaxRange;
            schedule = new SampleSchedule(config.UpdateRate);
        }

        public AltitudeReading Read(in DroneState truth, double time)
        {
            if (schedule.IsDue(time))
            {
                last = Sample(truth);
            }
            return last;
        }

        private AltitudeReading Sample(in DroneState truth)
        {
            var position = truth.Position;
            float heightAboveGround = MathF.Max(0f, position.Y - terrain.HeightAt(position.X, position.Z));
            return heightAboveGround > maxRange ? AltitudeReading.Invalid : new AltitudeReading(heightAboveGround, true);
        }
    }
}
