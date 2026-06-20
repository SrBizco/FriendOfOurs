using System.Collections.Generic;
using UnityEngine;

namespace FriendOfOurs.Gameplay
{
    public sealed class PlayerCombatController : MonoBehaviour
    {
        [Header("Unarmed Attack")]
        [SerializeField, Min(0f)] private float punchDamage = 25f;
        [SerializeField, Min(0.05f)] private float punchCooldown = 0.55f;
        [SerializeField, Min(0f)] private float punchHitDelay = 0.12f;
        [SerializeField, Min(0.05f)] private float comboResetTime = 0.9f;
        [SerializeField, Min(0.1f)] private float combatLayerHoldTime = 2f;
        [SerializeField, Min(0.1f)] private float punchRange = 1.4f;
        [SerializeField, Min(0.1f)] private float punchRadius = 0.65f;
        [SerializeField, Min(0f)] private float punchHeightOffset = 1.1f;
        [SerializeField] private Transform attackOrigin;
        [SerializeField] private LayerMask hitLayers = ~0;
        [SerializeField] private bool debugHits;

        [Header("Animation")]
        [SerializeField] private PlayerAnimationController animationController;

        private readonly Collider[] hits = new Collider[12];
        private readonly HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();
        private AttackComboCounter comboCounter;
        private Health health;
        private float nextPunchTime;
        private bool hasPendingHit;
        private float pendingHitTime;
        private float combatLayerReleaseTime;

        private void Awake()
        {
            if (animationController == null)
            {
                animationController = GetComponentInChildren<PlayerAnimationController>();
            }

            health = GetComponent<Health>();
            if (health == null)
            {
                health = gameObject.AddComponent<Health>();
            }

            comboCounter = new AttackComboCounter(3, comboResetTime);
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= OnDied;
            }
        }

        private void Update()
        {
            if (health != null && health.IsDead)
            {
                hasPendingHit = false;
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryPunch();
            }

            if (hasPendingHit && Time.time >= pendingHitTime)
            {
                hasPendingHit = false;
                ApplyPunchDamage();
            }

            if (animationController != null && combatLayerReleaseTime > 0f && Time.time >= combatLayerReleaseTime)
            {
                combatLayerReleaseTime = 0f;
                animationController.SetCombatLayerActive(false);
            }
        }

        private void TryPunch()
        {
            if (health != null && health.IsDead)
            {
                return;
            }

            if (Time.time < nextPunchTime)
            {
                return;
            }

            nextPunchTime = Time.time + punchCooldown;
            int attackIndex = comboCounter.NextAttackIndex(Time.time);
            animationController?.PlayUnarmedAttack(attackIndex);
            combatLayerReleaseTime = Time.time + combatLayerHoldTime;
            hasPendingHit = true;
            pendingHitTime = Time.time + punchHitDelay;
        }

        private void ApplyPunchDamage()
        {
            damagedTargets.Clear();
            Vector3 attackCenter = GetAttackCenter();
            int hitCount = Physics.OverlapSphereNonAlloc(
                attackCenter,
                punchRadius,
                hits,
                hitLayers,
                QueryTriggerInteraction.Ignore);

            if (debugHits)
            {
                Debug.Log($"Punch hit check at {attackCenter} found {hitCount} colliders.", this);
            }

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = hits[i];
                if (hit == null || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || damageable.IsDead || !damagedTargets.Add(damageable))
                {
                    if (debugHits)
                    {
                        Debug.Log($"Punch ignored {hit.name}: damageable={damageable != null}, dead={damageable?.IsDead}", hit);
                    }

                    continue;
                }

                Vector3 direction = hit.transform.position - transform.position;
                DamageInfo damageInfo = new DamageInfo(punchDamage, gameObject, hit.ClosestPoint(transform.position), direction);
                damageable.TakeDamage(damageInfo);

                if (debugHits)
                {
                    Debug.Log($"Punch damaged {hit.name} for {punchDamage}.", hit);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(GetAttackCenter(), punchRadius);
        }

        private Vector3 GetAttackCenter()
        {
            if (attackOrigin != null)
            {
                return attackOrigin.position + attackOrigin.forward * punchRange;
            }

            return AttackHitVolume.GetCenter(transform.position, transform.forward, punchHeightOffset, punchRange);
        }

        private void OnDied(DamageInfo damageInfo)
        {
            hasPendingHit = false;
            combatLayerReleaseTime = 0f;
            animationController?.SetCombatLayerActive(false);
        }
    }
}
