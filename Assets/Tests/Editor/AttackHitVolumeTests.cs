using FriendOfOurs.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace FriendOfOurs.Tests
{
    public sealed class AttackHitVolumeTests
    {
        [Test]
        public void GetCenter_UsesOriginHeightAndPlanarForward()
        {
            Vector3 center = AttackHitVolume.GetCenter(
                Vector3.zero,
                new Vector3(0f, 10f, 2f),
                1.25f,
                1.4f);

            Assert.That(center, Is.EqualTo(new Vector3(0f, 1.25f, 1.4f)));
        }

        [Test]
        public void GetCenter_FallsBackToWorldForwardWhenForwardIsZero()
        {
            Vector3 center = AttackHitVolume.GetCenter(
                Vector3.zero,
                Vector3.zero,
                1f,
                2f);

            Assert.That(center, Is.EqualTo(new Vector3(0f, 1f, 2f)));
        }
    }
}
