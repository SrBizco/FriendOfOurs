using FriendOfOurs.Gameplay;
using NUnit.Framework;

namespace FriendOfOurs.Tests
{
    public sealed class PlayerJumpCounterTests
    {
        [Test]
        public void TryConsumeJump_AllowsConfiguredNumberOfJumps()
        {
            PlayerJumpCounter counter = new PlayerJumpCounter(2);

            Assert.That(counter.TryConsumeJump(), Is.True);
            Assert.That(counter.TryConsumeJump(), Is.True);
            Assert.That(counter.TryConsumeJump(), Is.False);
        }

        [Test]
        public void Reset_RestoresAvailableJumps()
        {
            PlayerJumpCounter counter = new PlayerJumpCounter(1);

            Assert.That(counter.TryConsumeJump(), Is.True);
            Assert.That(counter.TryConsumeJump(), Is.False);

            counter.Reset(1);

            Assert.That(counter.TryConsumeJump(), Is.True);
        }
    }
}
