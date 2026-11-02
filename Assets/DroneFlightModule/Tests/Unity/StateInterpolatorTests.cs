using DroneFlightModule.Core;
using NUnit.Framework;
using UnityEngine;

namespace DroneFlightModule.Unity.Tests
{
    public class StateInterpolatorTests
    {
        private const float Tolerance = 1e-4f;

        private StateInterpolator interpolator;

        [SetUp]
        public void SetUp()
        {
            interpolator = new StateInterpolator();
            interpolator.Reset(DroneState.At(Vec3.Zero));
            interpolator.Push(DroneState.At(new Vec3(0f, 2f, 0f)));
        }

        [Test]
        public void BlendZero_ShowsPreviousTick()
        {
            Assert.That(interpolator.Sample(0f).position.y, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void BlendOne_ShowsLatestTick()
        {
            Assert.That(interpolator.Sample(1f).position.y, Is.EqualTo(2f).Within(Tolerance));
        }

        [Test]
        public void BlendHalf_ShowsMidpoint()
        {
            Assert.That(interpolator.Sample(0.5f).position.y, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void BlendOutsideRange_IsClamped()
        {
            Assert.That(interpolator.Sample(2f).position.y, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(interpolator.Sample(-1f).position.y, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Reset_SnapsWithoutBlending()
        {
            interpolator.Reset(DroneState.At(new Vec3(5f, 0f, 0f)));

            Assert.That(interpolator.Sample(0f).position.x, Is.EqualTo(5f).Within(Tolerance));
        }

        [Test]
        public void YawAcrossWrap_TakesShortWay()
        {
            interpolator.Reset(DroneState.At(Vec3.Zero, yaw: Mathf.PI - 0.1f));
            interpolator.Push(DroneState.At(Vec3.Zero, yaw: -Mathf.PI + 0.1f));

            var halfway = interpolator.Sample(0.5f).rotation * Vector3.forward;

            Assert.That(halfway.z, Is.EqualTo(-1f).Within(Tolerance));
        }

        [TestCase(10.00f, 10.00f, 0.02f, 0f)]
        [TestCase(10.01f, 10.00f, 0.02f, 0.5f)]
        [TestCase(10.05f, 10.00f, 0.02f, 1f)]
        [TestCase(10.00f, 10.00f, 0f, 1f)]
        public void BlendFactor_IsFractionOfFixedStepSinceLastTick(
            float time,
            float fixedTime,
            float fixedDeltaTime,
            float expected)
        {
            Assert.That(interpolator.BlendFactor(time, fixedTime, fixedDeltaTime), Is.EqualTo(expected).Within(Tolerance));
        }
    }
}
