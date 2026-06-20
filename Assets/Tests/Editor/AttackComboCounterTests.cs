using FriendOfOurs.Gameplay;
using NUnit.Framework;

namespace FriendOfOurs.Tests
{
    public sealed class AttackComboCounterTests
    {
        [Test]
        public void NextAttackIndex_AdvancesThroughComboAndWraps()
        {
            AttackComboCounter comboCounter = new AttackComboCounter(3, 1f);

            Assert.That(comboCounter.NextAttackIndex(0f), Is.EqualTo(0));
            Assert.That(comboCounter.NextAttackIndex(0.2f), Is.EqualTo(1));
            Assert.That(comboCounter.NextAttackIndex(0.4f), Is.EqualTo(2));
            Assert.That(comboCounter.NextAttackIndex(0.6f), Is.EqualTo(0));
        }

        [Test]
        public void NextAttackIndex_ResetsWhenTimeSinceLastAttackExceedsResetTime()
        {
            AttackComboCounter comboCounter = new AttackComboCounter(3, 0.5f);

            Assert.That(comboCounter.NextAttackIndex(0f), Is.EqualTo(0));
            Assert.That(comboCounter.NextAttackIndex(0.2f), Is.EqualTo(1));
            Assert.That(comboCounter.NextAttackIndex(0.8f), Is.EqualTo(0));
        }
    }
}
