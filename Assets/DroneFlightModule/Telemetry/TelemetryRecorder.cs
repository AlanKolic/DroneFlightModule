using System;
using System.Collections.Generic;
using DroneFlightModule.Core;

namespace DroneFlightModule.Telemetry
{
    /// <summary>
    /// One per drone. Samples its flight loop's ticks at the configured rate of simulation time,
    /// starting with the first tick, and passes each sample to every sink. Telemetry only reads: a sink that throws is reported
    /// through SinkFailed and skipped for that sample, so it can never break a tick or starve the
    /// other sinks. Disposing stops recording and ends the drone's stream in every sink.
    /// </summary>
    public sealed class TelemetryRecorder : IDisposable
    {
        private readonly FlightLoop loop;
        private readonly ITelemetrySink[] sinks;
        private readonly SampleSchedule schedule;
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
            schedule = new SampleSchedule(config.SampleRate);
            loop.Ticked += OnTicked;
        }

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
            if (!schedule.IsDue(frame.Time))
            {
                return;
            }
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
