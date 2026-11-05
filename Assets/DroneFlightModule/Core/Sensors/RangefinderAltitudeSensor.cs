using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// Measures height above the terrain directly below the drone, with a max range and a fixed
    /// update rate. Between samples it returns the last reading. The schedule advances by whole
    /// periods, so a tick step that does not divide the period still averages the update rate.
    /// No noise.
    /// </summary>
    public sealed class RangefinderAltitudeSensor : IAltitudeSensor
    {
        // Absorbs float drift when the tick step divides the sample period exactly.
        private const double TimeTolerance = 1e-6;

        private readonly ITerrain terrain;
        private readonly float maxRange;
        private readonly double samplePeriod;

        private bool hasSample;
        private double nextSampleTime;
        private AltitudeReading last;

        public RangefinderAltitudeSensor(ITerrain terrain, RangefinderConfig config)
        {
            this.terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            maxRange = config.MaxRange;
            samplePeriod = 1.0 / config.UpdateRate;
        }

        public AltitudeReading Read(in DroneState truth, double time)
        {
            if (hasSample && time < nextSampleTime - TimeTolerance)
            {
                return last;
            }

            last = Sample(truth, time);
            nextSampleTime = hasSample ? nextSampleTime + samplePeriod : time + samplePeriod;
            // A gap longer than a period starts the schedule again instead of sampling to catch up.
            if (nextSampleTime <= time)
            {
                nextSampleTime = time + samplePeriod;
            }
            hasSample = true;
            return last;
        }

        private AltitudeReading Sample(in DroneState truth, double time)
        {
            var position = truth.Position;
            float heightAboveGround = MathF.Max(0f, position.Y - terrain.HeightAt(position.X, position.Z));
            if (heightAboveGround > maxRange)
            {
                return AltitudeReading.Invalid(time);
            }
            return new AltitudeReading(heightAboveGround, true, time);
        }
    }
}
