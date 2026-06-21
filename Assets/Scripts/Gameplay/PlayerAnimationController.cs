using UnityEngine;

namespace FriendOfOurs.Gameplay
{
    public sealed class PlayerAnimationController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private string combatLayerName = "CombatUpperBody";
        [SerializeField] private float combatLayerBlendSpeed = 10f;
        [SerializeField] private string hitReactionLayerName = "HitReactionUpperBody";
        [SerializeField] private float hitReactionLayerBlendSpeed = 16f;
        [SerializeField, Min(0f)] private float hitReactionLayerHoldTime = 0.45f;
        [SerializeField] private string punchTrigger = "Punch";
        [SerializeField] private string leftHookTrigger = "LeftHook";
        [SerializeField] private string rightHookTrigger = "RightHook";
        [SerializeField] private string hitTrigger = "Hit";
        [SerializeField] private string deathTrigger = "Death";
        [SerializeField] private bool debugAnimatorParameters;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private int hitHash;
        private int deathHash;
        private int[] unarmedAttackHashes;
        private int combatLayerIndex = -1;
        private int hitReactionLayerIndex = -1;
        private float targetCombatLayerWeight;
        private float targetHitReactionLayerWeight;
        private float hitReactionLayerReleaseTime;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            unarmedAttackHashes = new[]
            {
                Animator.StringToHash(punchTrigger),
                Animator.StringToHash(leftHookTrigger),
                Animator.StringToHash(rightHookTrigger)
            };
            hitHash = Animator.StringToHash(hitTrigger);
            deathHash = Animator.StringToHash(deathTrigger);

            if (animator != null)
            {
                combatLayerIndex = animator.GetLayerIndex(combatLayerName);
                hitReactionLayerIndex = animator.GetLayerIndex(hitReactionLayerName);
                SetHitReactionLayerActive(false, true);
                ValidateAnimatorSetup();
            }
        }

        private void Update()
        {
            UpdateCombatLayerWeight();
            UpdateHitReactionLayerWeight();
        }

        public void SetLocomotion(float speed, bool isGrounded, float verticalSpeed)
        {
            if (animator == null)
            {
                return;
            }

            animator.SetFloat(SpeedHash, speed, 0.1f, Time.deltaTime);
            animator.SetBool(IsGroundedHash, isGrounded);
            animator.SetFloat(VerticalSpeedHash, verticalSpeed);
        }

        public void PlayJump()
        {
            if (animator == null)
            {
                return;
            }

            animator.SetTrigger(JumpHash);
        }

        public void PlayDeath()
        {
            if (animator == null)
            {
                return;
            }

            targetCombatLayerWeight = 0f;
            SetHitReactionLayerActive(false, true);
            animator.SetFloat(SpeedHash, 0f);
            animator.SetBool(IsGroundedHash, true);
            animator.SetFloat(VerticalSpeedHash, 0f);
            animator.SetTrigger(deathHash);
        }

        public void PlayHitReaction()
        {
            if (animator == null)
            {
                return;
            }

            SetHitReactionLayerActive(true, true);
            hitReactionLayerReleaseTime = Time.time + hitReactionLayerHoldTime;
            animator.SetTrigger(hitHash);
        }

        public void PlayUnarmedAttack(int comboIndex)
        {
            if (animator == null)
            {
                return;
            }

            int safeIndex = Mathf.Clamp(comboIndex, 0, unarmedAttackHashes.Length - 1);
            animator.SetTrigger(unarmedAttackHashes[safeIndex]);
            SetCombatLayerActive(true);
        }

        public void SetCombatLayerActive(bool isActive)
        {
            targetCombatLayerWeight = isActive ? 1f : 0f;
        }

        private void SetHitReactionLayerActive(bool isActive, bool immediate = false)
        {
            targetHitReactionLayerWeight = isActive ? 1f : 0f;

            if (immediate)
            {
                SetLayerWeightImmediate(hitReactionLayerIndex, targetHitReactionLayerWeight);
            }
        }

        private void UpdateCombatLayerWeight()
        {
            if (animator == null || combatLayerIndex < 0)
            {
                return;
            }

            float currentWeight = animator.GetLayerWeight(combatLayerIndex);
            float nextWeight = Mathf.MoveTowards(
                currentWeight,
                targetCombatLayerWeight,
                combatLayerBlendSpeed * Time.deltaTime);

            animator.SetLayerWeight(combatLayerIndex, nextWeight);
        }

        private void UpdateHitReactionLayerWeight()
        {
            if (animator == null || hitReactionLayerIndex < 0)
            {
                return;
            }

            if (hitReactionLayerReleaseTime > 0f && Time.time >= hitReactionLayerReleaseTime)
            {
                hitReactionLayerReleaseTime = 0f;
                SetHitReactionLayerActive(false);
            }

            float currentWeight = animator.GetLayerWeight(hitReactionLayerIndex);
            float nextWeight = Mathf.MoveTowards(
                currentWeight,
                targetHitReactionLayerWeight,
                hitReactionLayerBlendSpeed * Time.deltaTime);

            animator.SetLayerWeight(hitReactionLayerIndex, nextWeight);
        }

        private void SetLayerWeightImmediate(int layerIndex, float weight)
        {
            if (animator == null || layerIndex < 0)
            {
                return;
            }

            animator.SetLayerWeight(layerIndex, weight);
        }

        private void ValidateAnimatorSetup()
        {
            if (!debugAnimatorParameters || animator == null)
            {
                return;
            }

            LogMissingParameter(SpeedHash, "Speed");
            LogMissingParameter(IsGroundedHash, "IsGrounded");
            LogMissingParameter(VerticalSpeedHash, "VerticalSpeed");
            LogMissingParameter(JumpHash, "Jump");
            LogMissingParameter(hitHash, hitTrigger);
            LogMissingParameter(deathHash, deathTrigger);

            for (int i = 0; i < unarmedAttackHashes.Length; i++)
            {
                string parameterName = i == 0 ? punchTrigger : i == 1 ? leftHookTrigger : rightHookTrigger;
                LogMissingParameter(unarmedAttackHashes[i], parameterName);
            }

            if (combatLayerIndex < 0)
            {
                Debug.LogWarning($"Animator layer '{combatLayerName}' was not found.", this);
            }

            if (hitReactionLayerIndex < 0)
            {
                Debug.LogWarning($"Animator layer '{hitReactionLayerName}' was not found.", this);
            }
        }

        private void LogMissingParameter(int hash, string parameterName)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.nameHash == hash)
                {
                    return;
                }
            }

            Debug.LogWarning($"Animator parameter '{parameterName}' was not found.", this);
        }
    }
}
