using System;
using NUnit.Framework;

namespace DroneFlightModule.Core.Tests
{
    public class SampleScheduleTests
    {
        private int CountDue(SampleSchedule schedule, int ticks, double step)
        {
            int due = 0;
            double time = 0.0;
            for (int i = 0; i < ticks; i++)
            {
                if (schedule.IsDue(time))
                {
                    due++;
                }
                time += step;
            }
            return due;
        }

        [Test]
        public void TheFirstAsk_IsDue_ThenNotUntilAPeriodHasPassed()
        {
            var schedule = new SampleSchedule(rate: 10f);

            Assert.That(schedule.IsDue(1.0), Is.True);
            Assert.That(schedule.IsDue(1.05), Is.False);
            Assert.That(schedule.IsDue(1.1), Is.True);
        }

        [Test]
        public void FixedTicks_AreDueOncePerPeriod()
        {
            Assert.That(CountDue(new SampleSchedule(rate: 10f), ticks: 50, step: 0.02), Is.EqualTo(10));
        }

        [Test]
        public void AStepThatDoesNotDivideThePeriod_StillAveragesTheRate()
        {
            Assert.That(CountDue(new SampleSchedule(rate: 10f), ticks: 100, step: 0.03), Is.EqualTo(30), "3 s at 10 per second.");
        }

        [Test]
        public void AGapLongerThanAPeriod_StartsAgainInsteadOfCatchingUp()
        {
            var schedule = new SampleSchedule(rate: 10f);
            schedule.IsDue(0.0);

            Assert.That(schedule.IsDue(1.0), Is.True);
            Assert.That(schedule.IsDue(1.05), Is.False);
        }

        [TestCase(0f)]
        [TestCase(float.NaN)]
        public void Constructor_RejectsAnInvalidRate(float rate)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SampleSchedule(rate));
        }
    }
}
