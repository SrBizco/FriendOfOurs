namespace FriendOfOurs.Gameplay
{
    public sealed class PlayerJumpCounter
    {
        private int jumpsRemaining;

        public PlayerJumpCounter(int maxJumps)
        {
            Reset(maxJumps);
        }

        public bool TryConsumeJump()
        {
            if (jumpsRemaining <= 0)
            {
                return false;
            }

            jumpsRemaining--;
            return true;
        }

        public void Reset(int maxJumps)
        {
            jumpsRemaining = maxJumps < 0 ? 0 : maxJumps;
        }
    }
}
