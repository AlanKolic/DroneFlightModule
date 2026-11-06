using System;
using NUnit.Framework;

namespace DroneFlightModule.Core.Tests
{
    public class PatrolAutopilotTests
    {
        private sealed class SetClock : ISimulationClock
        {
            public double Time { get; set; }
        }

        private SetClock clock;
        private PatrolAutopilot autopilot;

        [SetUp]
        public void SetUp()
        {
            clock = new SetClock();
            autopilot = new PatrolAutopilot(clock);
            autopilot.Restart();
        }

        private Vec3 MoveAt(double time)
        {
            clock.Time = time;
            return autopilot.Read().Move;
        }

        [Test]
        public void RequiresAClock()
        {
            Assert.Throws<ArgumentNullException>(() => new PatrolAutopilot(null));
        }

        [Test]
        public void IsAlwaysConnected()
        {
            Assert.That(autopilot.IsConnected, Is.True);
        }

        [Test]
        public void ClimbsFirst()
        {
            Assert.That(MoveAt(0.0), Is.EqualTo(new Vec3(0f, 0.5f, 0f)));
            Assert.That(MoveAt(2.9), Is.EqualTo(new Vec3(0f, 0.5f, 0f)));
        }

        [Test]
        public void FliesTheFourLegs()
        {
            Assert.That(MoveAt(3.0), Is.EqualTo(new Vec3(0f, 0f, 0.5f)));
            Assert.That(MoveAt(6.0), Is.EqualTo(new Vec3(0.5f, 0f, 0f)));
            Assert.That(MoveAt(9.0), Is.EqualTo(new Vec3(0f, 0f, -0.5f)));
            Assert.That(MoveAt(12.0), Is.EqualTo(new Vec3(-0.5f, 0f, 0f)));
        }

        [Test]
        public void RepeatsWithoutClimbing()
        {
            Assert.That(MoveAt(15.0), Is.EqualTo(new Vec3(0f, 0f, 0.5f)));
        }

        [Test]
        public void Restart_ClimbsAgain()
        {
            clock.Time = 20.0;
            autopilot.Restart();

            Assert.That(MoveAt(21.0), Is.EqualTo(new Vec3(0f, 0.5f, 0f)));
        }

        [Test]
        public void NeverYaws()
        {
            clock.Time = 7.0;

            Assert.That(autopilot.Read().Yaw, Is.EqualTo(0f));
        }
    }
}
