using UnityEngine;
using UnityEngine.AI;
using FriendOfOurs.Gameplay;

namespace FriendOfOurs.NPCs
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class PedestrianController : MonoBehaviour
    {
        [Header("Wander")]
        [SerializeField, Min(1f)] private float routeStepDistance = 16f;
        [SerializeField, Range(0f, 90f)] private float routeDirectionJitter = 18f;
        [SerializeField, Min(0.1f)] private float destinationSampleRadius = 6f;
        [SerializeField, Min(0f)] private float minPauseTime = 0.2f;
        [SerializeField, Min(0f)] private float maxPauseTime = 0.8f;
        [SerializeField, Min(0.2f)] private float stuckCheckInterval = 1.5f;
        [SerializeField, Min(0.01f)] private float stuckMinimumMoveDistance = 0.25f;
        [SerializeField] private int areaMask = NavMesh.AllAreas;

        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float walkSpeed = 1.6f;
        [SerializeField, Min(0.1f)] private float runSpeed = 3.6f;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string speedParameter = "Speed";
        [SerializeField] private string hitTrigger = "Hit";
        [SerializeField] private string deathTrigger = "Death";
        [SerializeField] private string punchTrigger = "Punch";
        [SerializeField] private string leftHookTrigger = "LeftHook";
        [SerializeField] private string rightHookTrigger = "RightHook";
        [SerializeField] private string combatLayerName = "CombatUpperBody";
        [SerializeField, Min(0f)] private float combatLayerBlendSpeed = 10f;
        [SerializeField] private string hitReactionLayerName = "HitReactionUpperBody";
        [SerializeField, Min(0f)] private float hitReactionLayerBlendSpeed = 16f;
        [SerializeField, Min(0f)] private float hitReactionLayerHoldTime = 0.45f;
        [SerializeField, Range(0f, 1f)] private float walkAnimationSpeed = 0.5f;
        [SerializeField, Range(0f, 1f)] private float fleeAnimationSpeed = 1f;
        [SerializeField, Range(0f, 1f)] private float combatAnimationSpeed = 1f;
        [SerializeField, Min(0.05f)] private float comboResetTime = 4f;
        [SerializeField, Min(0f)] private float attackAnimationLockTime = 0.65f;

        [Header("Combat Reaction")]
        [SerializeField, Range(0f, 1f)] private float fightBackChance = 0.15f;
        [SerializeField, Min(0f)] private float minimumFleeDuration = 2f;
        [SerializeField, Min(1f)] private float fleeSafeDistance = 20f;
        [SerializeField, Min(0.1f)] private float fleeRepathInterval = 0.75f;
        [SerializeField, Min(0.1f)] private float fleeDistance = 12f;
        [SerializeField, Min(0.1f)] private float combatAttackRange = 1.8f;
        [SerializeField, Min(0.1f)] private float combatAttackRadius = 0.75f;
        [SerializeField, Min(0f)] private float combatAttackHeightOffset = 1.1f;
        [SerializeField, Min(0.1f)] private float combatStoppingDistance = 1.35f;
        [SerializeField, Min(0.1f)] private float combatAttackCooldown = 1.2f;
        [SerializeField, Min(0f)] private float combatDamage = 10f;
        [SerializeField, Min(1f)] private float combatGiveUpDistance = 9f;
        [SerializeField, Min(0f)] private float deadDespawnDelay = 5f;
        [SerializeField] private LayerMask combatHitLayers = ~0;
        [SerializeField] private bool debugCombat;

        private NavMeshAgent agent;
        private Collider bodyCollider;
        private Health health;
        private int speedHash;
        private int hitHash;
        private int deathHash;
        private int punchHash;
        private int[] attackHashes;
        private string[] attackTriggerNames;
        private int combatLayerIndex = -1;
        private int hitReactionLayerIndex = -1;
        private PedestrianStateMachine stateMachine;
        private PedestrianWanderState wanderState;
        private PedestrianPauseState pauseState;
        private PedestrianFleeState fleeState;
        private PedestrianCombatState combatState;
        private PedestrianDeadState deadState;
        private NavMeshPath routePath;
        private Vector3 routeDirection;
        private Vector3 lastStuckCheckPosition;
        private float nextStuckCheckTime;
        private float walkStoppingDistance;
        private float currentAnimationSpeedMax;
        private float attackAnimationLockEndTime;
        private float targetCombatLayerWeight;
        private float targetHitReactionLayerWeight;
        private float hitReactionLayerReleaseTime;
        private float deathTime;
        private Transform threat;
        private ThreatReaction threatReaction;
        private AttackComboCounter attackComboCounter;
        private readonly Collider[] combatHits = new Collider[8];

        public bool IsAgentReady => agent != null && agent.enabled && agent.isOnNavMesh;
        public bool HasThreat => threat != null;
        public float WalkSpeed => walkSpeed;
        public float MinimumFleeDuration => minimumFleeDuration;
        public float FleeSafeDistance => fleeSafeDistance;
        public float FleeRepathInterval => fleeRepathInterval;
        public float FleeSpeed => runSpeed;
        public float CombatSpeed => runSpeed;
        public float CombatAttackRange => combatAttackRange;
        public float CombatAttackCooldown => combatAttackCooldown;
        public float CombatGiveUpDistance => combatGiveUpDistance;
        public bool IsDead => health != null && health.IsDead;
        public bool CanDespawnDead => IsDead && Time.time >= deathTime + deadDespawnDelay;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            if (stateMachine != null && IsAgentReady)
            {
                EnterWanderState();
            }
        }

        private void Update()
        {
            stateMachine?.Tick();
            UpdateAnimation();
            UpdateCombatLayerWeight();
            UpdateHitReactionLayerWeight();
        }

        public void Initialize(Vector3 position, int navMeshAreaMask)
        {
            EnsureInitialized();
            areaMask = navMeshAreaMask;

            if (!agent.enabled)
            {
                agent.enabled = true;
            }

            if (bodyCollider != null)
            {
                bodyCollider.enabled = true;
            }

            health.ResetHealth();
            agent.Warp(position);
            threat = null;
            threatReaction = ThreatReaction.None;
            deathTime = 0f;
            attackAnimationLockEndTime = 0f;
            hitReactionLayerReleaseTime = 0f;
            ResetAnimatorForReuse();
            SetHitReactionLayerActive(false, true);
            BeginWalkMovement();
            ResetRouteDirection();
            lastStuckCheckPosition = position;
            nextStuckCheckTime = Time.time + stuckCheckInterval;
            EnterWanderState();
        }

        public void EnterWanderState()
        {
            stateMachine.ChangeState(wanderState);
        }

        public void EnterPauseState()
        {
            stateMachine.ChangeState(pauseState);
        }

        public void EnterFleeState()
        {
            stateMachine.ChangeState(fleeState);
        }

        public void EnterCombatState()
        {
            stateMachine.ChangeState(combatState);
        }

        public void EnterDeadState()
        {
            stateMachine.ChangeState(deadState);
        }

        public bool ShouldPauseAtDestination()
        {
            if (agent.pathPending)
            {
                return false;
            }

            if (!agent.hasPath)
            {
                return true;
            }

            float arrivalDistance = Mathf.Max(agent.stoppingDistance, 0.35f);
            return agent.remainingDistance <= arrivalDistance;
        }

        public bool IsStuck()
        {
            if (Time.time < nextStuckCheckTime || agent.pathPending)
            {
                return false;
            }

            bool movedEnough = PedestrianRouteRules.HasMovedEnough(
                lastStuckCheckPosition,
                transform.position,
                stuckMinimumMoveDistance);

            lastStuckCheckPosition = transform.position;
            nextStuckCheckTime = Time.time + stuckCheckInterval;

            return !movedEnough && agent.hasPath && agent.velocity.sqrMagnitude <= 0.04f;
        }

        public float GetRandomPauseDuration()
        {
            return Random.Range(minPauseTime, maxPauseTime);
        }

        public bool TryPickDestination()
        {
            if (routeDirection.sqrMagnitude <= 0.0001f)
            {
                ResetRouteDirection();
            }

            for (int i = 0; i < 14; i++)
            {
                Vector3 direction = GetRouteDirectionForAttempt(i);
                Vector3 candidate = PedestrianRouteRules.BuildCandidatePoint(transform.position, direction, routeStepDistance);

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, destinationSampleRadius, areaMask))
                {
                    if (routePath == null)
                    {
                        routePath = new NavMeshPath();
                    }

                    if (agent.CalculatePath(hit.position, routePath) && routePath.status == NavMeshPathStatus.PathComplete)
                    {
                        agent.SetDestination(hit.position);
                        routeDirection = PedestrianRouteRules.NormalizePlanarDirection(hit.position - transform.position, direction);
                        lastStuckCheckPosition = transform.position;
                        nextStuckCheckTime = Time.time + stuckCheckInterval;
                        return true;
                    }
                }
            }

            ResetRouteDirection();
            return false;
        }

        public void ResetRouteDirection()
        {
            Vector2 random = Random.insideUnitCircle.normalized;
            if (random.sqrMagnitude <= 0.0001f)
            {
                random = Vector2.up;
            }

            routeDirection = new Vector3(random.x, 0f, random.y);
        }

        public void SetAgentSpeed(float speed)
        {
            if (agent != null)
            {
                agent.speed = speed;
            }
        }

        public void BeginWalkMovement()
        {
            SetAgentStopped(false);
            SetAgentSpeed(WalkSpeed);
            SetAgentStoppingDistance(walkStoppingDistance);
            SetLocomotionAnimationMax(walkAnimationSpeed);
            SetCombatLayerActive(false);
        }

        public void BeginFleeMovement()
        {
            SetAgentStopped(false);
            SetAgentSpeed(runSpeed);
            SetAgentStoppingDistance(walkStoppingDistance);
            SetLocomotionAnimationMax(fleeAnimationSpeed);
            SetCombatLayerActive(false);
        }

        public void BeginCombatMovement()
        {
            SetAgentStopped(false);
            SetAgentSpeed(runSpeed);
            SetAgentStoppingDistance(combatStoppingDistance);
            SetLocomotionAnimationMax(combatAnimationSpeed);
            SetCombatLayerActive(true);
        }

        public bool IsThreatInAttackRange()
        {
            if (threat == null)
            {
                return false;
            }

            Vector3 toThreat = threat.position - transform.position;
            toThreat.y = 0f;
            return toThreat.magnitude <= combatAttackRange;
        }

        public bool IsThreatAlive()
        {
            if (threat == null)
            {
                return false;
            }

            IDamageable damageable = threat.GetComponentInParent<IDamageable>();
            return damageable == null || !damageable.IsDead;
        }

        public void ClearThreat()
        {
            threat = null;
            threatReaction = ThreatReaction.None;
            attackAnimationLockEndTime = 0f;
            SetCombatLayerActive(false);
        }

        public bool TryFleeFromThreat()
        {
            if (!IsAgentReady)
            {
                return false;
            }

            Vector3 awayDirection = threat != null
                ? transform.position - threat.position
                : routeDirection;

            awayDirection = PedestrianRouteRules.NormalizePlanarDirection(awayDirection, transform.forward);

            for (int i = 0; i < 8; i++)
            {
                float angle = i == 0 ? 0f : (i % 2 == 0 ? i * 18f : -i * 18f);
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * awayDirection;
                Vector3 candidate = PedestrianRouteRules.BuildCandidatePoint(transform.position, direction, fleeDistance);

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, destinationSampleRadius, areaMask))
                {
                    agent.SetDestination(hit.position);
                    return true;
                }
            }

            return false;
        }

        public float DistanceToThreat()
        {
            if (threat == null)
            {
                return float.MaxValue;
            }

            return Vector3.Distance(transform.position, threat.position);
        }

        public void MoveToThreat()
        {
            if (IsAgentReady && threat != null)
            {
                agent.isStopped = false;
                agent.SetDestination(threat.position);
            }
        }

        public void StopAgent()
        {
            if (IsAgentReady)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
        }

        public void FaceThreat()
        {
            if (threat == null)
            {
                return;
            }

            Vector3 direction = threat.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 720f * Time.deltaTime);
        }

        public void PunchThreat()
        {
            int attackIndex = attackComboCounter.NextAttackIndex(Time.time);
            SetCombatLayerActive(true);
            TrySetAnimatorTrigger(attackHashes[attackIndex], attackTriggerNames[attackIndex]);
            attackAnimationLockEndTime = Time.time + attackAnimationLockTime;

            Vector3 attackCenter = AttackHitVolume.GetCenter(
                transform.position,
                transform.forward,
                combatAttackHeightOffset,
                combatAttackRange);
            int hitCount = Physics.OverlapSphereNonAlloc(
                attackCenter,
                combatAttackRadius,
                combatHits,
                combatHitLayers,
                QueryTriggerInteraction.Ignore);

            if (debugCombat)
            {
                Debug.Log($"Pedestrian punch triggered at {attackCenter}. Found {hitCount} colliders.", this);
            }

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = combatHits[i];
                if (hit == null || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || damageable.IsDead)
                {
                    if (debugCombat)
                    {
                        Debug.Log($"Pedestrian punch ignored {hit.name}: damageable={damageable != null}, dead={damageable?.IsDead}.", hit);
                    }

                    continue;
                }

                Vector3 direction = hit.transform.position - transform.position;
                damageable.TakeDamage(new DamageInfo(combatDamage, gameObject, hit.ClosestPoint(transform.position), direction));
                if (debugCombat)
                {
                    Debug.Log($"Pedestrian punch damaged {hit.name} for {combatDamage}.", hit);
                }

                return;
            }
        }

        public void Die()
        {
            deathTime = Time.time;
            ClearThreat();

            if (agent != null)
            {
                agent.ResetPath();
                agent.enabled = false;
            }

            if (bodyCollider != null)
            {
                bodyCollider.enabled = false;
            }

            if (animator != null)
            {
                TrySetAnimatorTrigger(deathHash, deathTrigger);
            }
        }

        private void UpdateAnimation()
        {
            if (animator == null)
            {
                return;
            }

            float targetSpeed = 0f;
            if (agent != null && agent.enabled && agent.speed > 0.0001f)
            {
                float normalizedSpeed = Mathf.Clamp01(agent.velocity.magnitude / agent.speed);
                targetSpeed = normalizedSpeed * currentAnimationSpeedMax;
            }

            if (Time.time < attackAnimationLockEndTime)
            {
                targetSpeed = 0f;
            }

            animator.SetFloat(speedHash, targetSpeed, 0.15f, Time.deltaTime);
        }

        private void EnsureInitialized()
        {
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
                walkStoppingDistance = agent.stoppingDistance;
                currentAnimationSpeedMax = walkAnimationSpeed;
            }

            if (bodyCollider == null)
            {
                bodyCollider = GetComponent<Collider>();
            }

            if (health == null)
            {
                health = GetComponent<Health>();
                if (health == null)
                {
                    health = gameObject.AddComponent<Health>();
                }

                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator != null)
            {
                combatLayerIndex = animator.GetLayerIndex(combatLayerName);
                if (debugCombat && combatLayerIndex < 0)
                {
                    Debug.LogWarning($"Animator layer '{combatLayerName}' was not found on '{animator.name}'.", this);
                }

                hitReactionLayerIndex = animator.GetLayerIndex(hitReactionLayerName);
                if (debugCombat && hitReactionLayerIndex < 0)
                {
                    Debug.LogWarning($"Animator layer '{hitReactionLayerName}' was not found on '{animator.name}'.", this);
                }
            }

            speedHash = Animator.StringToHash(speedParameter);
            hitHash = Animator.StringToHash(hitTrigger);
            deathHash = Animator.StringToHash(deathTrigger);
            punchHash = Animator.StringToHash(punchTrigger);
            attackHashes = new[]
            {
                punchHash,
                Animator.StringToHash(leftHookTrigger),
                Animator.StringToHash(rightHookTrigger)
            };
            attackTriggerNames = new[]
            {
                punchTrigger,
                leftHookTrigger,
                rightHookTrigger
            };
            float effectiveComboResetTime = Mathf.Max(comboResetTime, combatAttackCooldown * attackHashes.Length + 0.05f);
            attackComboCounter = new AttackComboCounter(attackHashes.Length, effectiveComboResetTime);

            if (stateMachine == null)
            {
                stateMachine = new PedestrianStateMachine();
                wanderState = new PedestrianWanderState(this);
                pauseState = new PedestrianPauseState(this);
                fleeState = new PedestrianFleeState(this);
                combatState = new PedestrianCombatState(this);
                deadState = new PedestrianDeadState(this);
            }
        }

        private Vector3 GetRouteDirectionForAttempt(int attempt)
        {
            if (attempt == 0)
            {
                return ApplyJitter(routeDirection, routeDirectionJitter);
            }

            if (attempt == 1)
            {
                return -routeDirection;
            }

            float side = attempt % 2 == 0 ? 1f : -1f;
            float angle = routeDirectionJitter + attempt * 12f;
            return Quaternion.Euler(0f, side * angle, 0f) * routeDirection;
        }

        private static Vector3 ApplyJitter(Vector3 direction, float jitterDegrees)
        {
            float angle = Random.Range(-jitterDegrees, jitterDegrees);
            return Quaternion.Euler(0f, angle, 0f) * direction;
        }

        private void SetAgentStoppingDistance(float stoppingDistance)
        {
            if (agent != null)
            {
                agent.stoppingDistance = stoppingDistance;
            }
        }

        private void SetAgentStopped(bool stopped)
        {
            if (IsAgentReady)
            {
                agent.isStopped = stopped;
            }
        }

        private void SetLocomotionAnimationMax(float speed)
        {
            currentAnimationSpeedMax = Mathf.Clamp01(speed);
        }

        private void SetCombatLayerActive(bool isActive)
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

        private void ResetAnimatorForReuse()
        {
            if (animator == null)
            {
                return;
            }

            animator.Rebind();
            animator.Update(0f);
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

        private void PlayHitReaction()
        {
            if (animator == null)
            {
                return;
            }

            SetHitReactionLayerActive(true, true);
            hitReactionLayerReleaseTime = Time.time + hitReactionLayerHoldTime;
            TrySetAnimatorTrigger(hitHash, hitTrigger);
        }

        private bool TrySetAnimatorTrigger(int triggerHash, string triggerName)
        {
            if (animator == null)
            {
                if (debugCombat)
                {
                    Debug.LogWarning($"Cannot set animator trigger '{triggerName}' because no Animator is assigned.", this);
                }

                return false;
            }

            bool hasParameter = TryGetAnimatorParameter(triggerHash, out AnimatorControllerParameter parameter);
            if (debugCombat && !hasParameter)
            {
                Debug.LogWarning(BuildAnimatorParameterWarning(triggerName, "was not found"), this);
            }
            else if (debugCombat && parameter.type != AnimatorControllerParameterType.Trigger)
            {
                Debug.LogWarning(BuildAnimatorParameterWarning(triggerName, $"is {parameter.type}, expected Trigger"), this);
            }

            animator.SetTrigger(triggerHash);
            if (debugCombat)
            {
                string controllerName = animator.runtimeAnimatorController != null
                    ? animator.runtimeAnimatorController.name
                    : "None";

                Debug.Log($"Animator trigger '{triggerName}' sent to '{animator.name}' using controller '{controllerName}'.", this);
            }

            return hasParameter && parameter.type == AnimatorControllerParameterType.Trigger;
        }

        private bool TryGetAnimatorParameter(int parameterHash, out AnimatorControllerParameter foundParameter)
        {
            foundParameter = default;

            if (animator == null)
            {
                return false;
            }

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.nameHash == parameterHash)
                {
                    foundParameter = parameter;
                    return true;
                }
            }

            return false;
        }

        private string BuildAnimatorParameterWarning(string parameterName, string reason)
        {
            string animatorName = animator != null ? animator.name : "None";
            string controllerName = animator != null && animator.runtimeAnimatorController != null
                ? animator.runtimeAnimatorController.name
                : "None";

            return $"Animator parameter '{parameterName}' {reason}. Animator='{animatorName}', Controller='{controllerName}'.";
        }

        private void OnDamaged(DamageInfo damageInfo, float appliedDamage)
        {
            if (health.IsDead)
            {
                return;
            }

            PlayHitReaction();

            Transform attacker = damageInfo.Attacker != null ? damageInfo.Attacker.transform : null;
            if (attacker == null)
            {
                return;
            }

            if (threat != attacker)
            {
                threat = attacker;
                threatReaction = ThreatReaction.None;
            }

            if (threatReaction == ThreatReaction.None)
            {
                threatReaction = Random.value <= fightBackChance
                    ? ThreatReaction.Combat
                    : ThreatReaction.Flee;
            }

            if (threatReaction == ThreatReaction.Combat)
            {
                EnterCombatState();
            }
            else
            {
                EnterFleeState();
            }
        }

        private void OnDied(DamageInfo damageInfo)
        {
            threat = damageInfo.Attacker != null ? damageInfo.Attacker.transform : null;
            EnterDeadState();
        }

        private enum ThreatReaction
        {
            None,
            Flee,
            Combat
        }
    }
}
