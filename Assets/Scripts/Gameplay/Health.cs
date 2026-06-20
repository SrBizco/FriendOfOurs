using System;
using UnityEngine;

namespace FriendOfOurs.Gameplay
{
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;

        private HitPoints hitPoints;

        public event Action<DamageInfo, float> Damaged;
        public event Action<DamageInfo> Died;

        public float CurrentHealth => hitPoints != null ? hitPoints.Current : maxHealth;
        public float MaxHealth => hitPoints != null ? hitPoints.Maximum : maxHealth;
        public bool IsDead => hitPoints != null && hitPoints.IsDead;

        private void Awake()
        {
            hitPoints = new HitPoints(maxHealth, maxHealth);
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (hitPoints == null)
            {
                hitPoints = new HitPoints(maxHealth, maxHealth);
            }

            float appliedDamage = hitPoints.ApplyDamage(damageInfo.Amount);
            if (appliedDamage <= 0f)
            {
                return;
            }

            Damaged?.Invoke(damageInfo, appliedDamage);

            if (hitPoints.IsDead)
            {
                Died?.Invoke(damageInfo);
            }
        }

        public void ResetHealth()
        {
            if (hitPoints == null)
            {
                hitPoints = new HitPoints(maxHealth, maxHealth);
                return;
            }

            hitPoints.RestoreToFull();
        }
    }
}
