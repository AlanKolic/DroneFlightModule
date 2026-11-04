using System;
using NUnit.Framework;

namespace DroneFlightModule.Composition.Tests
{
    public class SimulationClockTests
    {
        [Test]
        public void Advance_AddsEachStep()
        {
            var clock = new SimulationClock();

            clock.Advance(0.5f);
            clock.Advance(0.25f);

            Assert.That(clock.Time, Is.EqualTo(0.75));
        }

        [Test]
        public void Advance_RejectsAStepThatIsNotPositive()
        {
            var clock = new SimulationClock();

            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Advance(0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Advance(float.NaN));
        }
    }
}
