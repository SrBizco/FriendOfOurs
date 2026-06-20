using UnityEngine;
using UnityEngine.AI;

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

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string speedParameter = "Speed";

        private NavMeshAgent agent;
        private int speedHash;
        private PedestrianStateMachine stateMachine;
        private PedestrianWanderState wanderState;
        private PedestrianPauseState pauseState;
        private NavMeshPath routePath;
        private Vector3 routeDirection;
        private Vector3 lastStuckCheckPosition;
        private float nextStuckCheckTime;

        public bool IsAgentReady => agent != null && agent.enabled && agent.isOnNavMesh;

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
        }

        public void Initialize(Vector3 position, int navMeshAreaMask)
        {
            EnsureInitialized();
            areaMask = navMeshAreaMask;

            agent.Warp(position);
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

        private void UpdateAnimation()
        {
            if (animator == null || agent.speed <= 0.0001f)
            {
                return;
            }

            float normalizedSpeed = Mathf.Clamp01(agent.velocity.magnitude / agent.speed);
            animator.SetFloat(speedHash, normalizedSpeed, 0.15f, Time.deltaTime);
        }

        private void EnsureInitialized()
        {
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            speedHash = Animator.StringToHash(speedParameter);

            if (stateMachine == null)
            {
                stateMachine = new PedestrianStateMachine();
                wanderState = new PedestrianWanderState(this);
                pauseState = new PedestrianPauseState(this);
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
    }
}
