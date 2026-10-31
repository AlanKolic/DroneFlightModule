using System;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// Measures height above the terrain directly below the drone, with a max range and a fixed
    /// update rate. Between samples it returns the last reading. No noise.
    /// </summary>
    public sealed class RangefinderAltitudeSensor : IAltitudeSensor
    {
        public const float DefaultMaxRange = 10f;
        public const float DefaultUpdateRate = 20f;

        // Absorbs float drift when the tick step divides the sample period exactly.
        private const double TimeTolerance = 1e-6;

        private readonly ITerrain terrain;
        private readonly float maxRange;
        private readonly double samplePeriod;

        private bool hasSample;
        private double nextSampleTime;
        private AltitudeReading last;

        public RangefinderAltitudeSensor(
            ITerrain terrain,
            float maxRange = DefaultMaxRange,
            float updateRate = DefaultUpdateRate)
        {
            this.terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            if (!(maxRange > 0f) || float.IsInfinity(maxRange))
            {
                throw new ArgumentOutOfRangeException(nameof(maxRange), maxRange, "Must be positive and finite.");
            }
            if (!(updateRate > 0f) || float.IsInfinity(updateRate))
            {
                throw new ArgumentOutOfRangeException(nameof(updateRate), updateRate, "Must be positive and finite.");
            }
            this.maxRange = maxRange;
            samplePeriod = 1.0 / updateRate;
        }

        public AltitudeReading Read(in DroneState truth, double time)
        {
            if (hasSample && time < nextSampleTime - TimeTolerance)
            {
                return last;
            }

            last = Sample(truth, time);
            hasSample = true;
            nextSampleTime = time + samplePeriod;
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
