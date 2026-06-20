using FriendOfOurs.NPCs;
using NUnit.Framework;
using UnityEngine;

namespace FriendOfOurs.Tests
{
    public sealed class PedestrianSpawnRulesTests
    {
        [Test]
        public void IsVisibleInViewport_ReturnsTrueForPointInsideCameraView()
        {
            bool visible = PedestrianSpawnRules.IsVisibleInViewport(new Vector3(0.5f, 0.5f, 10f), 0.05f);

            Assert.That(visible, Is.True);
        }

        [Test]
        public void IsVisibleInViewport_ReturnsFalseForPointBehindCamera()
        {
            bool visible = PedestrianSpawnRules.IsVisibleInViewport(new Vector3(0.5f, 0.5f, -1f), 0.05f);

            Assert.That(visible, Is.False);
        }

        [Test]
        public void IsWithinSpawnRing_UsesHorizontalDistance()
        {
            bool inRing = PedestrianSpawnRules.IsWithinSpawnRing(
                new Vector3(0f, 0f, 0f),
                new Vector3(10f, 100f, 0f),
                5f,
                15f);

            Assert.That(inRing, Is.True);
        }

        [Test]
        public void ShouldDespawn_ReturnsTrueOnlyWhenFarAndNotVisible()
        {
            Assert.That(PedestrianSpawnRules.ShouldDespawn(80f, false, 60f), Is.True);
            Assert.That(PedestrianSpawnRules.ShouldDespawn(80f, true, 60f), Is.False);
            Assert.That(PedestrianSpawnRules.ShouldDespawn(30f, false, 60f), Is.False);
        }
    }
}
