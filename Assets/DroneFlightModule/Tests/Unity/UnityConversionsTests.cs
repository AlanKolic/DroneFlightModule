using DroneFlightModule.Core;
using NUnit.Framework;
using UnityEngine;

namespace DroneFlightModule.Unity.Tests
{
    public class UnityConversionsTests
    {
        private const float Tolerance = 1e-5f;

        [Test]
        public void Vectors_MapAxesDirectly()
        {
            var vector = new Vec3(1f, 2f, 3f);

            Assert.That(vector.ToVector3(), Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Assert.That(new Vector3(1f, 2f, 3f).ToVec3(), Is.EqualTo(vector));
        }

        [TestCase(0.5f)]
        [TestCase(-2f)]
        [TestCase(3f)]
        public void PoseRotation_MatchesCoreRotateYaw(float yaw)
        {
            var unity = DroneState.At(Vec3.Zero, yaw).ToPose().rotation * Vector3.forward;
            var core = Vec3.Forward.RotateYaw(yaw);

            Assert.That(unity.x, Is.EqualTo(core.X).Within(Tolerance));
            Assert.That(unity.y, Is.EqualTo(core.Y).Within(Tolerance));
            Assert.That(unity.z, Is.EqualTo(core.Z).Within(Tolerance));
        }

        [Test]
        public void PositiveYaw_TurnsForwardTowardsRight()
        {
            var turned = DroneState.At(Vec3.Zero, Mathf.PI / 2f).ToPose().rotation * Vector3.forward;

            Assert.That(turned.x, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(turned.z, Is.EqualTo(0f).Within(Tolerance));
        }
    }
}
