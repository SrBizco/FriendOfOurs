using UnityEngine;

namespace FriendOfOurs.Gameplay
{
    public sealed class PlayerAnimationController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private string combatLayerName = "CombatUpperBody";
        [SerializeField] private float combatLayerBlendSpeed = 10f;
        [SerializeField] private string punchTrigger = "Punch";
        [SerializeField] private string leftHookTrigger = "LeftHook";
        [SerializeField] private string rightHookTrigger = "RightHook";
        [SerializeField] private string deathTrigger = "Death";
        [SerializeField] private bool debugAnimatorParameters;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private int deathHash;
        private int[] unarmedAttackHashes;
        private int combatLayerIndex = -1;
        private float targetCombatLayerWeight;

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
            deathHash = Animator.StringToHash(deathTrigger);

            if (animator != null)
            {
                combatLayerIndex = animator.GetLayerIndex(combatLayerName);
                ValidateAnimatorSetup();
            }
        }

        private void Update()
        {
            UpdateCombatLayerWeight();
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
            animator.SetFloat(SpeedHash, 0f);
            animator.SetBool(IsGroundedHash, true);
            animator.SetFloat(VerticalSpeedHash, 0f);
            animator.SetTrigger(deathHash);
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
