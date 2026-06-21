using UnityEngine;

namespace FriendOfOurs.Traffic
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class NpcCarController : MonoBehaviour
    {
        [Header("Route")]
        [SerializeField] private TrafficLane lane;
        [SerializeField, Min(0.5f)] private float lookAheadDistance = 6f;
        [SerializeField, Min(2)] private int closestPointSamples = 48;
        [SerializeField] private bool loopLane;

        [Header("Driving")]
        [SerializeField, Min(0f)] private float targetSpeed = 8f;
        [SerializeField, Min(0f)] private float maxMotorTorque = 900f;
        [SerializeField, Min(0f)] private float speedLimitBrakeTorque = 120f;
        [SerializeField, Min(0f)] private float stopBrakeTorque = 1600f;
        [SerializeField, Min(0f)] private float maxSteerAngle = 32f;
        [SerializeField, Min(0f)] private float steerResponse = 120f;
        [SerializeField, Min(0f)] private float startupSettleTime = 0.35f;
        [SerializeField] private bool frontWheelDrive;
        [SerializeField] private bool rearWheelDrive = true;

        [Header("Wheel Colliders")]
        [SerializeField] private WheelCollider frontLeftWheel;
        [SerializeField] private WheelCollider frontRightWheel;
        [SerializeField] private WheelCollider rearLeftWheel;
        [SerializeField] private WheelCollider rearRightWheel;

        [Header("Wheel Visuals")]
        [SerializeField] private Transform frontLeftVisual;
        [SerializeField] private Transform frontRightVisual;
        [SerializeField] private Transform rearLeftVisual;
        [SerializeField] private Transform rearRightVisual;

        [Header("Debug")]
        [SerializeField] private bool debugSetup;
        [SerializeField] private bool disableDrivingForDebug;
        [SerializeField] private bool drawDebugGizmos = true;
        [SerializeField, Min(0.1f)] private float groundedDebugInterval = 1f;
        [SerializeField, Min(0f)] private float groundProbeExtraDistance = 0.25f;

        private Rigidbody body;
        private float currentSteerAngle;
        private Vector3 lastTargetPoint;
        private float nextGroundedDebugTime;
        private float startupEndTime;
        private bool originalUseGravity;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (body != null)
            {
                originalUseGravity = body.useGravity;
                startupEndTime = Time.time + startupSettleTime;

                if (startupSettleTime > 0f)
                {
                    body.useGravity = false;
                    body.velocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
            }

            ValidateSetup();
        }

        private void Reset()
        {
            body = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            LogGroundedState();

            if (IsSettlingAtStartup())
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                ApplyDrive(0f, stopBrakeTorque, 0f);
                return;
            }

            if (disableDrivingForDebug)
            {
                ApplyDrive(0f, stopBrakeTorque, 0f);
                return;
            }

            if (lane == null || lane.Length <= 0.0001f)
            {
                ApplyDrive(0f, stopBrakeTorque, 0f);
                return;
            }

            float closestDistance = lane.FindClosestDistance(transform.position, closestPointSamples);
            float targetDistance = closestDistance + lookAheadDistance;
            bool reachedEnd = targetDistance >= lane.Length;

            if (loopLane)
            {
                targetDistance %= lane.Length;
            }
            else
            {
                targetDistance = Mathf.Min(targetDistance, lane.Length);
            }

            Vector3 targetPoint = lane.GetPointAtDistance(targetDistance);
            lastTargetPoint = targetPoint;
            Vector3 directionToTarget = targetPoint - transform.position;
            float desiredSteer = TrafficSteeringMath.GetSignedSteerAngle(transform.forward, directionToTarget, maxSteerAngle);
            currentSteerAngle = Mathf.MoveTowards(
                currentSteerAngle,
                desiredSteer,
                steerResponse * Time.fixedDeltaTime);

            float currentSpeed = body != null ? body.velocity.magnitude : 0f;
            float throttle = TrafficSteeringMath.GetAccelerationInput(currentSpeed, targetSpeed);
            float brakeTorque = currentSpeed > targetSpeed ? speedLimitBrakeTorque : 0f;

            if (!loopLane && reachedEnd && closestDistance >= lane.Length - lookAheadDistance)
            {
                throttle = 0f;
                brakeTorque = stopBrakeTorque;
            }

            ApplyDrive(throttle * maxMotorTorque, brakeTorque, currentSteerAngle);
        }

        private void LateUpdate()
        {
            UpdateWheelVisual(frontLeftWheel, frontLeftVisual);
            UpdateWheelVisual(frontRightWheel, frontRightVisual);
            UpdateWheelVisual(rearLeftWheel, rearLeftVisual);
            UpdateWheelVisual(rearRightWheel, rearRightVisual);
        }

        private bool IsSettlingAtStartup()
        {
            if (body == null || startupSettleTime <= 0f)
            {
                return false;
            }

            if (Time.time < startupEndTime)
            {
                return true;
            }

            if (body.useGravity != originalUseGravity)
            {
                body.useGravity = originalUseGravity;
            }

            return false;
        }

        [ContextMenu("Snap To Lane Start")]
        private void SnapToLaneStart()
        {
            if (lane == null)
            {
                return;
            }

            transform.position = lane.GetPointAtDistance(0f);
            Vector3 direction = lane.GetDirectionAtDistance(0f);
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }

        private void ApplyDrive(float motorTorque, float brakeTorque, float steerAngle)
        {
            ApplySteer(frontLeftWheel, steerAngle);
            ApplySteer(frontRightWheel, steerAngle);

            ApplyMotor(frontLeftWheel, frontWheelDrive ? motorTorque : 0f);
            ApplyMotor(frontRightWheel, frontWheelDrive ? motorTorque : 0f);
            ApplyMotor(rearLeftWheel, rearWheelDrive ? motorTorque : 0f);
            ApplyMotor(rearRightWheel, rearWheelDrive ? motorTorque : 0f);

            ApplyBrake(frontLeftWheel, brakeTorque);
            ApplyBrake(frontRightWheel, brakeTorque);
            ApplyBrake(rearLeftWheel, brakeTorque);
            ApplyBrake(rearRightWheel, brakeTorque);
        }

        private static void ApplySteer(WheelCollider wheel, float steerAngle)
        {
            if (wheel != null)
            {
                wheel.steerAngle = steerAngle;
            }
        }

        private static void ApplyMotor(WheelCollider wheel, float motorTorque)
        {
            if (wheel != null)
            {
                wheel.motorTorque = motorTorque;
            }
        }

        private static void ApplyBrake(WheelCollider wheel, float brakeTorque)
        {
            if (wheel != null)
            {
                wheel.brakeTorque = brakeTorque;
            }
        }

        private static void UpdateWheelVisual(WheelCollider wheel, Transform visual)
        {
            if (wheel == null || visual == null)
            {
                return;
            }

            wheel.GetWorldPose(out Vector3 position, out Quaternion rotation);
            visual.SetPositionAndRotation(position, rotation);
        }

        private void ValidateSetup()
        {
            ValidateWheelPair(frontLeftWheel, frontLeftVisual, "Front Left");
            ValidateWheelPair(frontRightWheel, frontRightVisual, "Front Right");
            ValidateWheelPair(rearLeftWheel, rearLeftVisual, "Rear Left");
            ValidateWheelPair(rearRightWheel, rearRightVisual, "Rear Right");

            if (body != null && debugSetup)
            {
                Debug.Log(
                    $"NPC car '{name}' mass={body.mass}, drag={body.drag}, angularDrag={body.angularDrag}, " +
                    $"centerOfMass={body.centerOfMass}, rootScale={transform.lossyScale}.",
                    this);

                if (body.mass < 100f)
                {
                    Debug.LogWarning(
                        $"NPC car '{name}' Rigidbody mass is {body.mass}. WheelCollider suspension values are usually " +
                        "tuned for vehicle-scale masses. Use about 1000-1400 for a small car before tuning springs.",
                        this);
                }
            }
        }

        private void ValidateWheelPair(WheelCollider wheel, Transform visual, string label)
        {
            if (wheel == null)
            {
                Debug.LogWarning($"{label} WheelCollider is not assigned on '{name}'.", this);
                return;
            }

            if (visual == null)
            {
                Debug.LogWarning($"{label} wheel visual is not assigned on '{name}'.", this);
                return;
            }

            if (visual == wheel.transform || wheel.transform.IsChildOf(visual))
            {
                Debug.LogWarning(
                    $"{label} wheel visual '{visual.name}' is the WheelCollider transform or one of its parents. " +
                    "Use separate sibling objects: one empty for WheelCollider and one mesh transform for the visual wheel.",
                    this);
            }

            if (visual.IsChildOf(wheel.transform))
            {
                Debug.LogWarning(
                    $"{label} wheel visual '{visual.name}' is child of WheelCollider '{wheel.name}'. " +
                    "Use sibling objects so visual pose updates do not move the WheelCollider hierarchy.",
                    this);
            }

            if (debugSetup)
            {
                Debug.Log(
                    $"{label} wheel radius={wheel.radius}, suspensionDistance={wheel.suspensionDistance}, " +
                    $"worldPosition={wheel.transform.position}, lossyScale={wheel.transform.lossyScale}.",
                    wheel);
            }
        }

        private void LogGroundedState()
        {
            if (!debugSetup || Time.time < nextGroundedDebugTime)
            {
                return;
            }

            nextGroundedDebugTime = Time.time + groundedDebugInterval;
            LogWheelGrounded(frontLeftWheel, "FL");
            LogWheelGrounded(frontRightWheel, "FR");
            LogWheelGrounded(rearLeftWheel, "RL");
            LogWheelGrounded(rearRightWheel, "RR");
        }

        private void LogWheelGrounded(WheelCollider wheel, string label)
        {
            if (wheel == null)
            {
                return;
            }

            bool grounded = wheel.GetGroundHit(out WheelHit hit);
            string hitName = grounded && hit.collider != null ? hit.collider.name : "None";
            Transform wheelTransform = wheel.transform;
            Vector3 center = wheelTransform.TransformPoint(wheel.center);
            Vector3 down = -wheelTransform.up;
            float probeDistance = wheel.radius + wheel.suspensionDistance + groundProbeExtraDistance;
            bool rayHit = Physics.Raycast(
                center,
                down,
                out RaycastHit raycastHit,
                probeDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            string rayHitName = rayHit && raycastHit.collider != null ? raycastHit.collider.name : "None";
            int rayHitLayer = rayHit && raycastHit.collider != null ? raycastHit.collider.gameObject.layer : -1;
            string rayColliderType = rayHit && raycastHit.collider != null ? raycastHit.collider.GetType().Name : "None";
            bool rayColliderTrigger = rayHit && raycastHit.collider != null && raycastHit.collider.isTrigger;
            PhysicMaterial rayMaterial = rayHit && raycastHit.collider != null ? raycastHit.collider.sharedMaterial : null;
            string rayMaterialName = rayMaterial != null ? rayMaterial.name : "None";
            bool layersIgnoreCollision = rayHitLayer >= 0 && Physics.GetIgnoreLayerCollision(wheel.gameObject.layer, rayHitLayer);
            float wheelUpDot = Vector3.Dot(wheelTransform.up.normalized, Vector3.up);
            float surfaceUpDot = rayHit ? Vector3.Dot(raycastHit.normal.normalized, Vector3.up) : 0f;
            string diagnosis = GetWheelContactDiagnosis(
                grounded,
                rayHit,
                rayColliderTrigger,
                layersIgnoreCollision,
                raycastHit.distance,
                wheel.radius,
                wheel.suspensionDistance,
                surfaceUpDot,
                wheelUpDot);

            Debug.Log(
                $"{label} WheelContact | result={diagnosis} | grounded={grounded} | rayHit={rayHit} | " +
                $"target={rayHitName} | collider={rayColliderType} | trigger={rayColliderTrigger} | " +
                $"distance={(rayHit ? raycastHit.distance : 0f):0.00} | normal={raycastHit.normal} | normalDot={surfaceUpDot:0.00} | " +
                $"radius={wheel.radius:0.00} | suspension={wheel.suspensionDistance:0.00} | upDot={wheelUpDot:0.00} | " +
                $"ignoreLayers={layersIgnoreCollision} | material={rayMaterialName} | enabled={wheel.enabled} | active={wheel.gameObject.activeInHierarchy}",
                wheel);
        }

        private static string GetWheelContactDiagnosis(
            bool grounded,
            bool rayHit,
            bool rayColliderTrigger,
            bool layersIgnoreCollision,
            float rayDistance,
            float radius,
            float suspensionDistance,
            float surfaceUpDot,
            float wheelUpDot)
        {
            if (grounded)
            {
                return "OK";
            }

            if (!rayHit)
            {
                return "NO_COLLIDER_UNDER_WHEEL";
            }

            if (rayColliderTrigger)
            {
                return "ROAD_COLLIDER_IS_TRIGGER";
            }

            if (layersIgnoreCollision)
            {
                return "LAYER_COLLISION_DISABLED";
            }

            if (wheelUpDot < 0.75f)
            {
                return "WHEEL_COLLIDER_ROTATED_WRONG";
            }

            if (surfaceUpDot < 0.5f)
            {
                return "ROAD_SURFACE_NORMAL_NOT_UP";
            }

            float expectedReach = radius + suspensionDistance;
            if (rayDistance > expectedReach)
            {
                return "WHEEL_TOO_HIGH_OR_SUSPENSION_TOO_SHORT";
            }

            if (rayDistance < radius + suspensionDistance * 0.25f)
            {
                return "WHEEL_TOO_LOW_OR_FULLY_COMPRESSED_AT_START";
            }

            return "WHEELCOLLIDER_REJECTS_THIS_COLLIDER_TEST_WITH_BOXCOLLIDER";
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos)
            {
                return;
            }

            Gizmos.color = Color.green;
            DrawWheelGizmo(frontLeftWheel);
            DrawWheelGizmo(frontRightWheel);
            DrawWheelGizmo(rearLeftWheel);
            DrawWheelGizmo(rearRightWheel);

            if (lane != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(lastTargetPoint, 0.35f);
                Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, lastTargetPoint);
            }
        }

        private static void DrawWheelGizmo(WheelCollider wheel)
        {
            if (wheel == null)
            {
                return;
            }

            Transform wheelTransform = wheel.transform;
            Vector3 center = wheelTransform.TransformPoint(wheel.center);
            Gizmos.DrawWireSphere(center, wheel.radius);
            Gizmos.DrawLine(center, center - wheelTransform.up * wheel.suspensionDistance);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(center, center - wheelTransform.up * (wheel.radius + wheel.suspensionDistance + 0.25f));
        }
    }
}
