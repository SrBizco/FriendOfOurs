using FriendOfOurs.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace FriendOfOurs.Tests
{
    public sealed class TopDownControlMathTests
    {
        [Test]
        public void GetWorldMoveDirection_NormalizesDiagonalInput()
        {
            Vector3 direction = TopDownControlMath.GetWorldMoveDirection(new Vector2(1f, 1f));

            Assert.That(direction.y, Is.EqualTo(0f));
            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(direction.x, Is.GreaterThan(0f));
            Assert.That(direction.z, Is.GreaterThan(0f));
        }

        [Test]
        public void GetWorldMoveDirection_ReturnsZeroForNoInput()
        {
            Vector3 direction = TopDownControlMath.GetWorldMoveDirection(Vector2.zero);

            Assert.That(direction, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void GetCameraRelativeMoveDirection_MapsForwardInputToCameraForwardOnGround()
        {
            Vector3 direction = TopDownControlMath.GetCameraRelativeMoveDirection(
                new Vector2(0f, 1f),
                Vector3.right);

            Assert.That(direction.x, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(direction.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(direction.z, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void GetCameraRelativeMoveDirection_NormalizesDiagonalInput()
        {
            Vector3 direction = TopDownControlMath.GetCameraRelativeMoveDirection(
                new Vector2(1f, 1f),
                Vector3.forward);

            Assert.That(direction.y, Is.EqualTo(0f));
            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(direction.x, Is.GreaterThan(0f));
            Assert.That(direction.z, Is.GreaterThan(0f));
        }

        [Test]
        public void GetAnimationSpeed_ReturnsWalkingSpeedWhenMovingWithoutSprint()
        {
            float speed = TopDownControlMath.GetAnimationSpeed(1f, false);

            Assert.That(speed, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void GetAnimationSpeed_ReturnsRunningSpeedWhenMovingWithSprint()
        {
            float speed = TopDownControlMath.GetAnimationSpeed(1f, true);

            Assert.That(speed, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void GetCameraPosition_UsesYawHeightAndDistance()
        {
            Vector3 target = new Vector3(10f, 0f, 5f);

            Vector3 cameraPosition = TopDownControlMath.GetCameraPosition(target, 0f, 12f, 8f);

            Assert.That(cameraPosition.x, Is.EqualTo(10f).Within(0.0001f));
            Assert.That(cameraPosition.y, Is.EqualTo(8f).Within(0.0001f));
            Assert.That(cameraPosition.z, Is.EqualTo(-7f).Within(0.0001f));
        }

        [Test]
        public void GetGroundCheckPosition_UsesColliderBottomWhenColliderExists()
        {
            Bounds bounds = new Bounds(new Vector3(0f, 1f, 0f), new Vector3(1f, 2f, 1f));

            Vector3 checkPosition = TopDownControlMath.GetGroundCheckPosition(
                Vector3.zero,
                bounds,
                0.25f);

            Assert.That(checkPosition, Is.EqualTo(new Vector3(0f, 0.25f, 0f)));
        }
    }
}
