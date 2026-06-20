using FriendOfOurs.NPCs;
using NUnit.Framework;
using UnityEngine;

namespace FriendOfOurs.Tests
{
    public sealed class PedestrianRouteRulesTests
    {
        [Test]
        public void NormalizePlanarDirection_ReturnsFallbackWhenDirectionIsZero()
        {
            Vector3 direction = PedestrianRouteRules.NormalizePlanarDirection(Vector3.zero, Vector3.forward);

            Assert.That(direction, Is.EqualTo(Vector3.forward));
        }

        [Test]
        public void BuildCandidatePoint_UsesOnlyPlanarDirection()
        {
            Vector3 candidate = PedestrianRouteRules.BuildCandidatePoint(
                Vector3.zero,
                new Vector3(0f, 10f, 2f),
                5f);

            Assert.That(candidate, Is.EqualTo(new Vector3(0f, 0f, 5f)));
        }

        [Test]
        public void HasMovedEnough_UsesHorizontalDistance()
        {
            bool moved = PedestrianRouteRules.HasMovedEnough(
                Vector3.zero,
                new Vector3(0.4f, 10f, 0f),
                0.5f);

            Assert.That(moved, Is.False);
        }
    }
}
