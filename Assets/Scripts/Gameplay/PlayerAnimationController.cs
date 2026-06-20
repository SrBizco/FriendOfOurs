using UnityEngine;

namespace FriendOfOurs.Gameplay
{
    public sealed class PlayerAnimationController : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
        private static readonly int JumpHash = Animator.StringToHash("Jump");

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
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
    }
}
