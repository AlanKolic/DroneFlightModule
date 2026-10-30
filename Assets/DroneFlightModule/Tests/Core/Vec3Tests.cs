using System;
using NUnit.Framework;

namespace DroneFlightModule.Core.Tests
{
    public class Vec3Tests
    {
        private const float Tolerance = 1e-5f;

        [Test]
        public void RotateYaw_QuarterTurn_TurnsForwardToRight()
        {
            var rotated = Vec3.Forward.RotateYaw(MathF.PI / 2f);

            Assert.That(rotated.X, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(rotated.Y, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(rotated.Z, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void RotateYaw_QuarterTurn_TurnsRightToBack()
        {
            var rotated = Vec3.Right.RotateYaw(MathF.PI / 2f);

            Assert.That(rotated.X, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(rotated.Z, Is.EqualTo(-1f).Within(Tolerance));
        }

        [Test]
        public void RotateYaw_KeepsVerticalComponent()
        {
            var rotated = new Vec3(1f, 7f, 2f).RotateYaw(1.234f);

            Assert.That(rotated.Y, Is.EqualTo(7f));
        }
    }
}
