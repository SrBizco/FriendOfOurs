using UnityEngine;

namespace FriendOfOurs.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float walkSpeed = 4f;
        [SerializeField, Min(0f)] private float runSpeed = 7f;
        [SerializeField, Min(0f)] private float rotationSpeed = 720f;
        [SerializeField] private Transform cameraTransform;

        [Header("Jump")]
        [SerializeField, Min(0)] private int maxConsecutiveJumps = 1;
        [SerializeField, Min(0f)] private float jumpForce = 6f;
        [SerializeField, Min(0f)] private float groundCheckRadius = 0.25f;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private LayerMask groundLayers = ~0;

        [Header("Animation")]
        [SerializeField] private PlayerAnimationController animationController;

        private Rigidbody body;
        private PlayerJumpCounter jumpCounter;
        private PlayerStateMachine stateMachine;
        private Vector3 moveDirection;
        private Vector2 moveInput;
        private bool jumpRequested;
        private bool isGrounded;
        private bool wasGrounded;
        private bool isSprinting;
        private readonly Collider[] groundHits = new Collider[8];

        public PlayerIdleState IdleState { get; private set; }
        public PlayerMoveState MoveState { get; private set; }
        public PlayerJumpState JumpState { get; private set; }
        public bool HasMoveInput => moveDirection.sqrMagnitude > 0.0001f;
        public bool IsGrounded => isGrounded;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;

            if (animationController == null)
            {
                animationController = GetComponentInChildren<PlayerAnimationController>();
            }

            jumpCounter = new PlayerJumpCounter(maxConsecutiveJumps);
            stateMachine = new PlayerStateMachine();
            IdleState = new PlayerIdleState(this);
            MoveState = new PlayerMoveState(this);
            JumpState = new PlayerJumpState(this);
        }

        private void Start()
        {
            stateMachine.ChangeState(IdleState);
        }

        private void Update()
        {
            moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            isSprinting = Input.GetKey(KeyCode.LeftShift);
            jumpRequested |= Input.GetButtonDown("Jump");

            Transform referenceTransform = cameraTransform != null ? cameraTransform : Camera.main != null ? Camera.main.transform : null;

            moveDirection = referenceTransform != null
                ? TopDownControlMath.GetCameraRelativeMoveDirection(moveInput, referenceTransform.forward)
                : TopDownControlMath.GetWorldMoveDirection(moveInput);

            UpdateGroundedState();
            UpdateAnimation();
            stateMachine.Tick();
        }

        private void FixedUpdate()
        {
            stateMachine.FixedTick();
        }

        public bool ConsumeJumpInput()
        {
            if (!jumpRequested)
            {
                return false;
            }

            jumpRequested = false;
            return true;
        }

        public void TryJump()
        {
            if (!jumpCounter.TryConsumeJump())
            {
                return;
            }

            ChangeState(JumpState);
        }

        public void ApplyJumpImpulse()
        {
            Vector3 velocity = body.velocity;
            velocity.y = 0f;
            body.velocity = velocity;
            body.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);

            if (animationController != null)
            {
                animationController.PlayJump();
            }
        }

        public void ApplyHorizontalMovement()
        {
            float currentSpeed = isSprinting ? runSpeed : walkSpeed;
            Vector3 nextPosition = body.position + moveDirection * (currentSpeed * Time.fixedDeltaTime);
            body.MovePosition(nextPosition);
        }

        public void ApplyMovementRotation()
        {
            if (moveDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            Quaternion nextRotation = Quaternion.RotateTowards(
                body.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime);

            body.MoveRotation(nextRotation);
        }

        public void ChangeState(PlayerState nextState)
        {
            stateMachine.ChangeState(nextState);
        }

        private void UpdateGroundedState()
        {
            Vector3 checkPosition = groundCheck != null
                ? groundCheck.position
                : transform.position + Vector3.down * 0.9f;

            wasGrounded = isGrounded;
            int hitCount = Physics.OverlapSphereNonAlloc(
                checkPosition,
                groundCheckRadius,
                groundHits,
                groundLayers,
                QueryTriggerInteraction.Ignore);

            isGrounded = false;
            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = groundHits[i];
                if (hit != null && !hit.transform.IsChildOf(transform))
                {
                    isGrounded = true;
                    break;
                }
            }

            if (isGrounded && !wasGrounded)
            {
                jumpCounter.Reset(maxConsecutiveJumps);
            }
        }

        private void UpdateAnimation()
        {
            if (animationController == null)
            {
                return;
            }

            float animationSpeed = TopDownControlMath.GetAnimationSpeed(moveInput.magnitude, isSprinting);
            animationController.SetLocomotion(animationSpeed, isGrounded, body.velocity.y);
        }
    }
}
