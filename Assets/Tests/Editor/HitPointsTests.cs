using FriendOfOurs.Gameplay;
using NUnit.Framework;

namespace FriendOfOurs.Tests
{
    public sealed class HitPointsTests
    {
        [Test]
        public void Constructor_ClampsCurrentHealthToMaximum()
        {
            HitPoints hitPoints = new HitPoints(100f, 250f);

            Assert.That(hitPoints.Current, Is.EqualTo(100f));
            Assert.That(hitPoints.Maximum, Is.EqualTo(100f));
            Assert.That(hitPoints.IsDead, Is.False);
        }

        [Test]
        public void ApplyDamage_ReducesHealthAndReturnsAppliedAmount()
        {
            HitPoints hitPoints = new HitPoints(100f, 100f);

            float applied = hitPoints.ApplyDamage(35f);

            Assert.That(applied, Is.EqualTo(35f));
            Assert.That(hitPoints.Current, Is.EqualTo(65f));
            Assert.That(hitPoints.IsDead, Is.False);
        }

        [Test]
        public void ApplyDamage_ClampsAtZeroAndMarksDead()
        {
            HitPoints hitPoints = new HitPoints(100f, 25f);

            float applied = hitPoints.ApplyDamage(60f);

            Assert.That(applied, Is.EqualTo(25f));
            Assert.That(hitPoints.Current, Is.Zero);
            Assert.That(hitPoints.IsDead, Is.True);
        }

        [Test]
        public void RestoreToFull_RevivesDeadHealth()
        {
            HitPoints hitPoints = new HitPoints(100f, 25f);
            hitPoints.ApplyDamage(25f);

            hitPoints.RestoreToFull();

            Assert.That(hitPoints.Current, Is.EqualTo(100f));
            Assert.That(hitPoints.IsDead, Is.False);
        }
    }
}
