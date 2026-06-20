using UnityEngine;

namespace FriendOfOurs.Gameplay
{
    public sealed class AttackComboCounter
    {
        private readonly int comboLength;
        private readonly float resetTime;
        private int nextIndex;
        private float lastAttackTime = float.NegativeInfinity;

        public AttackComboCounter(int comboLength, float resetTime)
        {
            this.comboLength = Mathf.Max(1, comboLength);
            this.resetTime = Mathf.Max(0.01f, resetTime);
        }

        public int NextAttackIndex(float time)
        {
            if (time - lastAttackTime > resetTime)
            {
                nextIndex = 0;
            }

            int currentIndex = nextIndex;
            nextIndex = (nextIndex + 1) % comboLength;
            lastAttackTime = time;
            return currentIndex;
        }
    }
}
