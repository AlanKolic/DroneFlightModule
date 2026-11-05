using System;
using System.Collections.Generic;
using DroneFlightModule.Core;
using NUnit.Framework;

namespace DroneFlightModule.Telemetry.Tests
{
    public class TelemetryRecorderTests
    {
        private readonly TestLoops loops = new TestLoops();
        private readonly TelemetryConfig tenPerSecond = new TelemetryConfig(sampleRate: 10f);

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
        public void BeforeTheFirstTick_NothingIsWritten()
        {
            Recorder(tenPerSecond, sink);

            Assert.That(sink.Written, Is.Empty);
        }

        [Test]
        public void TheFirstTick_IsSampled()
        {
            Recorder(tenPerSecond, sink);

            loops.Run(loop, 1);

            Assert.That(sink.Written, Has.Count.EqualTo(1));
            Assert.That(sink.Written[0].Time, Is.EqualTo(TestLoops.DeltaTime).Within(1e-6));
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
        public void AStepThatDoesNotDivideThePeriod_StillAveragesTheRate()
        {
            Recorder(tenPerSecond, sink);

            for (int i = 0; i < 100; i++)
            {
                loop.Tick(0.03f);
            }

            Assert.That(sink.Written, Has.Count.EqualTo(30), "3 s at 10 samples per second.");
        }

        [Test]
        public void ATickLongerThanAPeriod_SamplesOnceAndCarriesOn()
        {
            Recorder(tenPerSecond, sink);

            loop.Tick(1f);
            loop.Tick(0.05f);
            loop.Tick(0.05f);

            Assert.That(sink.Written, Has.Count.EqualTo(2));
            Assert.That(sink.Written[1].Time, Is.EqualTo(1.1).Within(1e-6));
        }

        [Test]
        public void ASample_IsTheFrameOfItsTick()
        {
            Recorder(new TelemetryConfig(sampleRate: 50f), sink);

            loops.Run(loop, 5);

            var latest = sink.Written[sink.Written.Count - 1];
            Assert.That(latest.Time, Is.EqualTo(loop.Time).Within(1e-6));
            Assert.That(latest.State.Position, Is.EqualTo(loop.State.Position));
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
