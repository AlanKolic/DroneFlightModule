using System;
using NUnit.Framework;
using UnityEngine;

namespace DroneFlightModule.Unity.Tests
{
    public class ChaseCameraRigTests
    {
        private const float Tolerance = 1e-3f;

        private static readonly Vector3 Offset = new Vector3(0f, 2f, -6f);

        private ChaseCameraRig rig;

        [SetUp]
        public void SetUp()
        {
            rig = new ChaseCameraRig(Offset, lookHeight: 0.5f, sharpness: 5f);
        }

        private void AssertNear(Vector3 actual, Vector3 expected)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(Tolerance), $"{actual} vs {expected}");
        }

        [Test]
        public void Constructor_RejectsNonPositiveSharpness()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChaseCameraRig(Offset, 0.5f, 0f));
        }

        [Test]
        public void Snap_SitsAtOffsetAndLooksAtTarget()
        {
            var target = new Pose(new Vector3(1f, 3f, 2f), Quaternion.identity);

            var pose = rig.Snap(target);

            AssertNear(pose.position, target.position + Offset);
            var toTarget = (target.position + Vector3.up * 0.5f - pose.position).normalized;
            AssertNear(pose.rotation * Vector3.forward, toTarget);
        }

        [Test]
        public void Snap_OffsetTurnsWithTargetHeading()
        {
            var facingRight = new Pose(Vector3.zero, Quaternion.AngleAxis(90f, Vector3.up));

            var pose = rig.Snap(facingRight);

            AssertNear(pose.position, new Vector3(-6f, 2f, 0f));
        }

        [Test]
        public void Follow_ZeroDeltaTime_DoesNotMove()
        {
            var camera = new Pose(new Vector3(5f, 5f, 5f), Quaternion.identity);

            var pose = rig.Follow(camera, new Pose(Vector3.zero, Quaternion.identity), 0f);

            AssertNear(pose.position, camera.position);
        }

        [Test]
        public void Follow_ConvergesOnSnapPose()
        {
            var target = new Pose(new Vector3(10f, 4f, -3f), Quaternion.AngleAxis(45f, Vector3.up));
            var camera = new Pose(Vector3.zero, Quaternion.identity);

            for (int i = 0; i < 300; i++)
            {
                camera = rig.Follow(camera, target, 1f / 60f);
            }

            AssertNear(camera.position, rig.Snap(target).position);
        }

        [Test]
        public void Follow_IsFrameRateIndependent()
        {
            var target = new Pose(new Vector3(10f, 0f, 0f), Quaternion.identity);
            var start = new Pose(Vector3.zero, Quaternion.identity);

            var oneStep = rig.Follow(start, target, 0.1f);
            var tenSteps = start;
            for (int i = 0; i < 10; i++)
            {
                tenSteps = rig.Follow(tenSteps, target, 0.01f);
            }

            AssertNear(tenSteps.position, oneStep.position);
        }
    }
}
