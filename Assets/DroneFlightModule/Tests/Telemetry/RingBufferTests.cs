using System;
using System.Linq;
using NUnit.Framework;

namespace DroneFlightModule.Telemetry.Tests
{
    public class RingBufferTests
    {
        [Test]
        public void Capacity_MustBePositive()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RingBuffer<int>(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RingBuffer<int>(-1));
        }

        [Test]
        public void UntilFull_KeepsEverythingOldestFirst()
        {
            var buffer = new RingBuffer<int>(3);

            buffer.Add(1);
            buffer.Add(2);

            Assert.That(buffer.Count, Is.EqualTo(2));
            Assert.That(buffer[0], Is.EqualTo(1));
            Assert.That(buffer[1], Is.EqualTo(2));
        }

        [Test]
        public void OnceFull_OverwritesTheOldest()
        {
            var buffer = new RingBuffer<int>(3);

            for (int i = 1; i <= 5; i++)
            {
                buffer.Add(i);
            }

            Assert.That(buffer.Count, Is.EqualTo(3));
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 3, 4, 5 }));
            Assert.That(buffer.Capacity, Is.EqualTo(3));
        }

        [Test]
        public void AnIndexOutsideTheCount_Throws()
        {
            var buffer = new RingBuffer<int>(3);
            buffer.Add(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => _ = buffer[1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = buffer[-1]);
        }
    }
}
