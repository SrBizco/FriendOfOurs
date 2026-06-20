using UnityEngine;

namespace FriendOfOurs.Gameplay
{
    public readonly struct DamageInfo
    {
        public DamageInfo(float amount, GameObject attacker, Vector3 point, Vector3 direction)
        {
            Amount = amount;
            Attacker = attacker;
            Point = point;
            Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
        }

        public float Amount { get; }
        public GameObject Attacker { get; }
        public Vector3 Point { get; }
        public Vector3 Direction { get; }
    }
}
