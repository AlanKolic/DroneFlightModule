using NUnit.Framework;

namespace DroneFlightModule.Core.Tests
{
    public class ControlInputTests
    {
        [Test]
        public void Constructor_ClampsEveryAxisToUnitRange()
        {
            var input = new ControlInput(new Vec3(2f, -3f, 0.5f), -9f);

            Assert.That(input.Move.X, Is.EqualTo(1f));
            Assert.That(input.Move.Y, Is.EqualTo(-1f));
            Assert.That(input.Move.Z, Is.EqualTo(0.5f));
            Assert.That(input.Yaw, Is.EqualTo(-1f));
        }

        [Test]
        public void Constructor_TreatsNaNAsZero()
        {
            var input = new ControlInput(new Vec3(float.NaN, float.NaN, float.NaN), float.NaN);

            Assert.That(input.Move, Is.EqualTo(Vec3.Zero));
            Assert.That(input.Yaw, Is.EqualTo(0f));
        }
    }
}
