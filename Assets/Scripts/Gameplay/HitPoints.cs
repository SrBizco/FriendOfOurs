using UnityEngine;

namespace FriendOfOurs.Gameplay
{
    public sealed class HitPoints
    {
        public HitPoints(float maximum, float current)
        {
            Maximum = Mathf.Max(1f, maximum);
            Current = Mathf.Clamp(current, 0f, Maximum);
        }

        public float Maximum { get; }
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;

        public float ApplyDamage(float amount)
        {
            if (IsDead || amount <= 0f)
            {
                return 0f;
            }

            float previous = Current;
            Current = Mathf.Max(0f, Current - amount);
            return previous - Current;
        }

        public void RestoreToFull()
        {
            Current = Maximum;
        }
    }
}
