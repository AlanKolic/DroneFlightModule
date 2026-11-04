using System;
using System.Collections.Generic;
using DroneFlightModule.Core;
using NUnit.Framework;

namespace DroneFlightModule.Telemetry.Tests
{
    public class TelemetryRecorderTests
    {
        private readonly TestLoops loops = new TestLoops();
        private readonly TelemetryConfig tenPerSecond = new TelemetryConfig(sampleRate: 10f, droneHistoryLength: 100, fleetHistoryLength: 100);

        private FlightLoop loop;
        private RecordingSink sink;

        [SetUp]
        public void SetUp()
        {
            loop = loops.Hovering("drone-1");
            sink = new RecordingSink();
        }

        private TelemetryRecorder Recorder(TelemetryConfig config, params ITelemetrySink[] sinks)
        {
            return new TelemetryRecorder(loop, config, sinks);
        }

        [Test]
        public void Constructor_RequiresItsParts()
        {
            Assert.Throws<ArgumentNullException>(() => new TelemetryRecorder(null, tenPerSecond, new ITelemetrySink[0]));
            Assert.Throws<ArgumentNullException>(() => new TelemetryRecorder(loop, null, new ITelemetrySink[0]));
            Assert.Throws<ArgumentNullException>(() => new TelemetryRecorder(loop, tenPerSecond, null));
            Assert.Throws<ArgumentException>(() => new TelemetryRecorder(loop, tenPerSecond, new ITelemetrySink[] { null }));
        }

        [Test]
        public void BeforeTheFirstTick_HasNoSample()
        {
            var recorder = Recorder(tenPerSecond, sink);

            Assert.That(recorder.HasSample, Is.False);
            Assert.That(recorder.History, Is.Empty);
            Assert.That(recorder.DroneId, Is.EqualTo(loop.DroneId));
        }

        [Test]
        public void TheFirstTick_IsSampled()
        {
            var recorder = Recorder(tenPerSecond, sink);

            loops.Run(loop, 1);

            Assert.That(recorder.HasSample, Is.True);
            Assert.That(recorder.Latest.Time, Is.EqualTo(TestLoops.DeltaTime).Within(1e-6));
        }

        [Test]
        public void Samples_AtTheConfiguredRateOfSimulationTime()
        {
            Recorder(tenPerSecond, sink);

            loops.Run(loop, 50);

            Assert.That(sink.Written, Has.Count.EqualTo(10));
            for (int i = 1; i < sink.Written.Count; i++)
            {
                Assert.That(sink.Written[i].Time - sink.Written[i - 1].Time, Is.EqualTo(0.1).Within(1e-6));
            }
        }

        [Test]
        public void KeepsTheLatestSample_AndAsManyRecentOnesAsConfigured()
        {
            var recorder = Recorder(new TelemetryConfig(sampleRate: 50f, droneHistoryLength: 3, fleetHistoryLength: 10), sink);

            loops.Run(loop, 5);

            Assert.That(recorder.History, Has.Count.EqualTo(3));
            Assert.That(recorder.History[0].Time, Is.EqualTo(3 * TestLoops.DeltaTime).Within(1e-6));
            Assert.That(recorder.Latest.Time, Is.EqualTo(loop.Time).Within(1e-6));
            Assert.That(recorder.Latest.State.Position, Is.EqualTo(loop.State.Position));
        }

        [Test]
        public void EverySink_GetsEverySample()
        {
            var other = new RecordingSink();
            Recorder(tenPerSecond, sink, other);

            loops.Run(loop, 50);

            Assert.That(other.Written, Is.EqualTo(sink.Written));
        }

        [Test]
        public void AFailingSink_IsReported_WithoutStoppingTheFlightOrTheOtherSinks()
        {
            var failing = new ThrowingSink();
            var failures = new List<ITelemetrySink>();
            var recorder = Recorder(tenPerSecond, failing, sink);
            recorder.SinkFailed += (failed, _) => failures.Add(failed);

            Assert.DoesNotThrow(() => loops.Run(loop, 50));

            Assert.That(sink.Written, Has.Count.EqualTo(10));
            Assert.That(failures, Has.Count.EqualTo(10));
            Assert.That(failures[0], Is.SameAs(failing));
        }

        [Test]
        public void Dispose_StopsRecording_AndEndsTheStreamInEverySink()
        {
            var recorder = Recorder(tenPerSecond, new ThrowingSink(), sink);
            loops.Run(loop, 1);

            recorder.Dispose();
            loops.Run(loop, 50);
            recorder.Dispose();

            Assert.That(sink.Written, Has.Count.EqualTo(1));
            Assert.That(sink.Ended, Is.EqualTo(new[] { loop.DroneId }));
        }
    }
}
