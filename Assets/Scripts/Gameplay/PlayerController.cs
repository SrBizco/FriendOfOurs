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

        [Header("Slope Limit")]
        [SerializeField, Range(0f, 89f)] private float maxSlopeAngle = 40f;
        [SerializeField, Min(0.1f)] private float slopeProbeForwardDistance = 0.9f;
        [SerializeField, Min(0.1f)] private float slopeProbeHeight = 2f;

        [Header("Jump")]
        [SerializeField, Min(0)] private int maxConsecutiveJumps = 1;
        [SerializeField, Min(0f)] private float jumpForce = 6f;
        [SerializeField, Min(0f)] private float groundCheckRadius = 0.25f;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private LayerMask groundLayers = ~0;

        [Header("Animation")]
        [SerializeField] private PlayerAnimationController animationController;

        private Rigidbody body;
        private Collider bodyCollider;
        private Health health;
        private PlayerJumpCounter jumpCounter;
        private PlayerStateMachine stateMachine;
        private Vector3 moveDirection;
        private Vector2 moveInput;
        private bool jumpRequested;
        private bool isGrounded;
        private bool wasGrounded;
        private bool isSprinting;
        private bool isDead;
        private readonly Collider[] groundHits = new Collider[8];

        public PlayerIdleState IdleState { get; private set; }
        public PlayerMoveState MoveState { get; private set; }
        public PlayerJumpState JumpState { get; private set; }
        public PlayerDeadState DeadState { get; private set; }
        public bool IsDead => isDead;
        public bool HasMoveInput => moveDirection.sqrMagnitude > 0.0001f;
        public bool IsGrounded => isGrounded;
        public bool CanFinishJump => isGrounded && body.velocity.y <= 0.1f;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            bodyCollider = GetComponent<Collider>();
            health = GetComponent<Health>();
            if (health == null)
            {
                health = gameObject.AddComponent<Health>();
            }

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
            DeadState = new PlayerDeadState(this);
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }
        }

        private void Start()
        {
            stateMachine.ChangeState(IdleState);
        }

        private void Update()
        {
            if (isDead)
            {
                UpdateGroundedState();
                stateMachine.Tick();
                return;
            }

            moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                isSprinting = !isSprinting;
            }
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

            if (stateMachine.CurrentState == JumpState)
            {
                ApplyJumpImpulse();
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
            if (!CanMoveInDirection(moveDirection))
            {
                return;
            }

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

        public void StopMovement(bool resetSprint = true)
        {
            moveInput = Vector2.zero;
            moveDirection = Vector3.zero;
            jumpRequested = false;
            if (resetSprint)
            {
                isSprinting = false;
            }

            if (body != null)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        public void PlayDeathAnimation()
        {
            animationController?.PlayDeath();
        }

        public bool RespawnAt(Transform respawnPoint)
        {
            if (respawnPoint == null || health == null)
            {
                return false;
            }

            StopMovement();
            body.position = respawnPoint.position;
            body.rotation = respawnPoint.rotation;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            health.ResetHealth();
            jumpCounter.Reset(maxConsecutiveJumps);
            isDead = false;
            isGrounded = false;
            wasGrounded = false;
            animationController?.ResetForRespawn();
            ChangeState(IdleState);
            return true;
        }

        private void UpdateGroundedState()
        {
            Vector3 checkPosition = GetGroundCheckPosition();

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

        private Vector3 GetGroundCheckPosition()
        {
            if (groundCheck != null)
            {
                return groundCheck.position;
            }

            if (bodyCollider != null)
            {
                return TopDownControlMath.GetGroundCheckPosition(
                    transform.position,
                    bodyCollider.bounds,
                    groundCheckRadius);
            }

            return transform.position + Vector3.down * groundCheckRadius;
        }

        private bool CanMoveInDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude <= 0.0001f || maxSlopeAngle >= 89f)
            {
                return true;
            }

            Vector3 normalizedDirection = direction.normalized;
            Vector3 probeOrigin = body.position
                + Vector3.up * slopeProbeHeight
                + normalizedDirection * slopeProbeForwardDistance;

            float probeDistance = slopeProbeHeight + slopeProbeForwardDistance + groundCheckRadius;
            if (!Physics.Raycast(
                    probeOrigin,
                    Vector3.down,
                    out RaycastHit groundHit,
                    probeDistance,
                    groundLayers,
                    QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            float slopeAngle = Vector3.Angle(groundHit.normal, Vector3.up);
            if (slopeAngle <= maxSlopeAngle)
            {
                return true;
            }

            Vector3 uphillDirection = Vector3.ProjectOnPlane(Vector3.up, groundHit.normal);
            if (uphillDirection.sqrMagnitude <= 0.0001f)
            {
                return true;
            }

            return Vector3.Dot(normalizedDirection, uphillDirection.normalized) <= 0.01f;
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

        private void OnDied(DamageInfo damageInfo)
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            ChangeState(DeadState);
        }

        private void OnDamaged(DamageInfo damageInfo, float appliedDamage)
        {
            if (isDead || health.IsDead)
            {
                return;
            }

            animationController?.PlayHitReaction();
        }
    }
}
