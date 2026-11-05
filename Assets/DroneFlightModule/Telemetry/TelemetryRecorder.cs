using System;
using System.Collections.Generic;
using DroneFlightModule.Core;

namespace DroneFlightModule.Telemetry
{
    /// <summary>
    /// One per drone. Samples its flight loop's ticks at the configured rate of simulation time,
    /// starting with the first tick, keeps the latest sample and passes each to every sink. The
    /// schedule advances by whole periods, so a tick step that does not divide the period still
    /// averages the configured rate. Telemetry only reads: a sink that throws is reported
    /// through SinkFailed and skipped for that sample, so it can never break a tick or starve the
    /// other sinks. Disposing stops recording and ends the drone's stream in every sink.
    /// </summary>
    public sealed class TelemetryRecorder : IDisposable
    {
        // Absorbs float drift when the tick step divides the sample period exactly.
        private const double TimeTolerance = 1e-6;

        private readonly FlightLoop loop;
        private readonly ITelemetrySink[] sinks;
        private readonly double samplePeriod;
        private double nextSampleTime;
        private bool isDisposed;

        public TelemetryRecorder(FlightLoop loop, TelemetryConfig config, IReadOnlyList<ITelemetrySink> sinks)
        {
            this.loop = loop ?? throw new ArgumentNullException(nameof(loop));
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            if (sinks == null)
            {
                throw new ArgumentNullException(nameof(sinks));
            }

            this.sinks = new ITelemetrySink[sinks.Count];
            for (int i = 0; i < sinks.Count; i++)
            {
                this.sinks[i] = sinks[i] ?? throw new ArgumentException("Sinks cannot be null.", nameof(sinks));
            }
            samplePeriod = 1.0 / config.SampleRate;
            loop.Ticked += OnTicked;
        }

        public DroneId DroneId => loop.DroneId;
        public bool HasSample { get; private set; }
        public FlightFrame Latest { get; private set; }
        public event Action<ITelemetrySink, Exception> SinkFailed;

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }
            isDisposed = true;
            loop.Ticked -= OnTicked;
            foreach (var sink in sinks)
            {
                try
                {
                    sink.End(loop.DroneId);
                }
                catch (Exception exception)
                {
                    SinkFailed?.Invoke(sink, exception);
                }
            }
        }

        private void OnTicked(FlightFrame frame)
        {
            if (HasSample && frame.Time < nextSampleTime - TimeTolerance)
            {
                return;
            }
            nextSampleTime = HasSample ? nextSampleTime + samplePeriod : frame.Time + samplePeriod;
            // A tick longer than a period starts the schedule again instead of sampling to catch up.
            if (nextSampleTime <= frame.Time)
            {
                nextSampleTime = frame.Time + samplePeriod;
            }
            HasSample = true;
            Latest = frame;
            foreach (var sink in sinks)
            {
                try
                {
                    sink.Write(frame);
                }
                catch (Exception exception)
                {
                    SinkFailed?.Invoke(sink, exception);
                }
            }
        }
    }
}
