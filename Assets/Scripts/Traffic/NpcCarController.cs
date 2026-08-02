using UnityEngine;

namespace FriendOfOurs.Traffic
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(VehicleObstacleSensor))]
    public sealed class NpcCarController : MonoBehaviour
    {
        [Header("Route")]
        [SerializeField] private TrafficNetwork trafficNetwork;
        [SerializeField, Min(0.5f)] private float lookAheadDistance = 7f;
        [SerializeField, Min(2)] private int pathSamples = 28;
        [SerializeField] private bool autoDetectStartingRoad = true;
        [SerializeField, Min(0.1f)] private float maxStartingRoadDetectionDistance = 9f;

        [Header("Driving")]
        [SerializeField, Min(0f)] private float targetSpeed = 8f;
        [SerializeField, Min(0.1f)] private float turnSpeed = 4.5f;
        [SerializeField, Min(0f)] private float maxMotorTorque = 900f;
        [SerializeField, Min(0f)] private float speedLimitBrakeTorque = 120f;
        [SerializeField, Min(0f)] private float turnBrakeTorque = 800f;
        [SerializeField, Min(0f)] private float obstacleBrakeTorque = 650f;
        [SerializeField, Min(0f)] private float stopBrakeTorque = 1600f;
        [SerializeField, Min(0f)] private float emergencyBrakeTorque = 3000f;
        [SerializeField, Min(0f)] private float maxSteerAngle = 32f;
        [SerializeField, Min(0f)] private float steerResponse = 120f;
        [SerializeField] private bool frontWheelDrive;
        [SerializeField] private bool rearWheelDrive = true;

        [Header("Traffic awareness")]
        [SerializeField] private VehicleObstacleSensor obstacleSensor;

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
        [SerializeField] private bool drawDebugGizmos = true;
        [SerializeField] private bool disableNpcDriving;

        private Rigidbody body;
        private TrafficPath activePath;
        private int currentEdgeIndex = -1;
        private int pendingEdgeIndex = -1;
        private bool isTurning;
        private bool routeInitialized;
        private float currentSteerAngle;
        private Vector3 lastTargetPoint;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            obstacleSensor = obstacleSensor != null ? obstacleSensor : GetComponent<VehicleObstacleSensor>();
        }

        private void OnEnable()
        {
            if (body != null)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        public void InitializeTrafficRoute(TrafficNetwork network, int edgeIndex)
        {
            trafficNetwork = network;
            currentEdgeIndex = edgeIndex;
            pendingEdgeIndex = -1;
            isTurning = false;
            routeInitialized = trafficNetwork != null && trafficNetwork.IsValidEdgeIndex(edgeIndex);
            if (routeInitialized)
            {
                activePath = trafficNetwork.GetRoadPath(currentEdgeIndex);
                Vector3 forward = activePath.GetDirection(0f);
                lastTargetPoint = activePath.Evaluate(0f);
                transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            }
        }

        /// <summary>Call this when the player steals or leaves the vehicle.</summary>
        public void SetNpcDrivingEnabled(bool enabled)
        {
            disableNpcDriving = !enabled;
            if (!enabled) ApplyDrive(0f, stopBrakeTorque, 0f);
        }

        private void FixedUpdate()
        {
            if (disableNpcDriving)
            {
                ApplyDrive(0f, stopBrakeTorque, 0f);
                return;
            }

            ResolveNetworkAndRoute();
            if (!routeInitialized)
            {
                ApplyDrive(0f, stopBrakeTorque, 0f);
                return;
            }

            float pathLength = Mathf.Max(0.1f, activePath.Length);
            float closestT = activePath.FindClosestT(transform.position, pathSamples);
            // Change to the intersection curve before reaching the end of the road segment.
            // Waiting until the bumper reaches the end makes a WheelCollider vehicle drive straight through the junction.
            float turnPreparationT = Mathf.Clamp01(1f - lookAheadDistance / pathLength);
            if (closestT >= turnPreparationT)
            {
                AdvanceRoute();
                pathLength = Mathf.Max(0.1f, activePath.Length);
                closestT = activePath.FindClosestT(transform.position, pathSamples);
            }

            float lookAheadT = Mathf.Clamp01(closestT + lookAheadDistance / pathLength);
            lastTargetPoint = activePath.Evaluate(lookAheadT);
            Vector3 directionToTarget = lastTargetPoint - transform.position;
            float desiredSteer = TrafficSteeringMath.GetSignedSteerAngle(transform.forward, directionToTarget, maxSteerAngle);
            currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, desiredSteer, steerResponse * Time.fixedDeltaTime);

            float currentSpeed = body.velocity.magnitude;
            float routeSpeed = Mathf.Min(targetSpeed, trafficNetwork.SpeedLimit);
            if (isTurning)
            {
                routeSpeed = Mathf.Min(routeSpeed, turnSpeed);
            }
            TrafficObstacleResponse obstacle = obstacleSensor != null ? obstacleSensor.Scan() : TrafficObstacleResponse.Clear;
            routeSpeed *= obstacle.SpeedFactor;
            float throttle = TrafficSteeringMath.GetAccelerationInput(currentSpeed, routeSpeed);
            float brake = TrafficSteeringMath.GetBrakeTorque(currentSpeed, routeSpeed, obstacle.HasObstacle, obstacle.ShouldStop,
                speedLimitBrakeTorque, obstacleBrakeTorque, stopBrakeTorque);
            if (obstacle.ShouldStop)
            {
                brake = Mathf.Max(brake, emergencyBrakeTorque);
            }
            if (isTurning && currentSpeed > routeSpeed)
            {
                brake = Mathf.Max(brake, turnBrakeTorque);
            }
            ApplyDrive(throttle * maxMotorTorque, brake, currentSteerAngle);
        }

        private void ResolveNetworkAndRoute()
        {
            if (trafficNetwork == null) trafficNetwork = TrafficNetwork.Active;
            if (routeInitialized || trafficNetwork == null) return;
            if (!autoDetectStartingRoad) return;

            if (trafficNetwork.TryFindClosestEdge(transform.position, transform.forward, maxStartingRoadDetectionDistance, out int edgeIndex))
            {
                currentEdgeIndex = edgeIndex;
                activePath = trafficNetwork.GetRoadPath(edgeIndex);
                routeInitialized = true;
            }
        }

        private void AdvanceRoute()
        {
            if (!isTurning)
            {
                if (!trafficNetwork.TrySelectNextEdge(currentEdgeIndex, out pendingEdgeIndex))
                {
                    routeInitialized = false;
                    return;
                }

                activePath = trafficNetwork.GetTurnPath(currentEdgeIndex, pendingEdgeIndex);
                isTurning = true;
                return;
            }

            currentEdgeIndex = pendingEdgeIndex;
            pendingEdgeIndex = -1;
            activePath = trafficNetwork.GetRoadPath(currentEdgeIndex);
            isTurning = false;
        }

        private void LateUpdate()
        {
            UpdateWheelVisual(frontLeftWheel, frontLeftVisual);
            UpdateWheelVisual(frontRightWheel, frontRightVisual);
            UpdateWheelVisual(rearLeftWheel, rearLeftVisual);
            UpdateWheelVisual(rearRightWheel, rearRightVisual);
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

        private static void ApplySteer(WheelCollider wheel, float value) { if (wheel != null) wheel.steerAngle = value; }
        private static void ApplyMotor(WheelCollider wheel, float value) { if (wheel != null) wheel.motorTorque = value; }
        private static void ApplyBrake(WheelCollider wheel, float value) { if (wheel != null) wheel.brakeTorque = value; }

        private static void UpdateWheelVisual(WheelCollider wheel, Transform visual)
        {
            if (wheel == null || visual == null) return;
            wheel.GetWorldPose(out Vector3 position, out Quaternion rotation);
            visual.SetPositionAndRotation(position, rotation);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos) return;
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(lastTargetPoint, 0.3f);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.35f, lastTargetPoint);
        }
    }
}
