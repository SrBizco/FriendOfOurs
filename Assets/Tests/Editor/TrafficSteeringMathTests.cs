using FriendOfOurs.Traffic;
using NUnit.Framework;
using UnityEngine;

namespace FriendOfOurs.Tests
{
    public sealed class TrafficSteeringMathTests
    {
        [Test]
        public void GetSignedSteerAngle_ReturnsPositiveAngleForTargetToTheRight()
        {
            float angle = TrafficSteeringMath.GetSignedSteerAngle(
                Vector3.forward,
                Vector3.right,
                30f);

            Assert.That(angle, Is.EqualTo(30f).Within(0.0001f));
        }

        [Test]
        public void GetSignedSteerAngle_ClampsAngleToMaximum()
        {
            float angle = TrafficSteeringMath.GetSignedSteerAngle(
                Vector3.forward,
                new Vector3(1f, 0f, 1f),
                20f);

            Assert.That(angle, Is.EqualTo(20f).Within(0.0001f));
        }

        [Test]
        public void GetAccelerationInput_ReturnsFullThrottleBelowTargetSpeed()
        {
            float input = TrafficSteeringMath.GetAccelerationInput(4f, 10f);

            Assert.That(input, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void GetAccelerationInput_ReturnsNoThrottleAtTargetSpeed()
        {
            float input = TrafficSteeringMath.GetAccelerationInput(10.5f, 10f);

            Assert.That(input, Is.EqualTo(0f).Within(0.0001f));
        }
    }
}
