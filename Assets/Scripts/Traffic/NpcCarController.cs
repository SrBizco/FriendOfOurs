using UnityEngine;
using FriendOfOurs.Audio;

namespace FriendOfOurs.Traffic
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(VehicleObstacleSensor))]
    public sealed class NpcCarController : MonoBehaviour
    {
        private const float RoadDetectionRetryDelay = 0.5f;

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

        [Header("Player driving")]
        [SerializeField] private bool isDecorativeVehicle;
        [SerializeField, Min(0f)] private float playerMotorTorque = 1500f;
        [SerializeField, Min(0f)] private float playerReverseTorque = 850f;
        [SerializeField, Min(0f)] private float playerBrakeTorque = 2800f;
        [SerializeField, Min(0f)] private float playerHandbrakeTorque = 4200f;
        [SerializeField, Min(0f)] private float playerArcadeAcceleration = 7.5f;
        [SerializeField, Min(0f)] private float playerArcadeBrakeDeceleration = 18f;
        [SerializeField, Min(0f)] private float playerArcadeReverseAcceleration = 5f;
        [SerializeField, Min(0.1f)] private float playerMaxForwardSpeed = 18f;
        [SerializeField, Min(0.1f)] private float playerMaxReverseSpeed = 7f;
        [SerializeField, Range(0.1f, 0.95f)] private float playerAccelerationFalloffStart = 0.55f;
        [SerializeField, Range(0.05f, 1f)] private float playerTopSpeedAccelerationFactor = 0.25f;
        [SerializeField, Min(0f)] private float playerMaxSteerAngle = 36f;
        [SerializeField, Min(0f)] private float playerHighSpeedSteerAngle = 22f;
        [SerializeField, Min(0.1f)] private float playerSteerReductionSpeed = 18f;
        [SerializeField, Min(0f)] private float playerSteerResponse = 160f;
        [SerializeField, Range(0.1f, 1f)] private float playerDriftRearGrip = 0.55f;
        [SerializeField, Min(0f)] private float playerGripChangeSpeed = 5f;
        [SerializeField, Min(0f)] private float playerLateralGripAssist = 1.5f;
        [SerializeField, Min(0f)] private float playerNormalYawAssist = 1.2f;
        [SerializeField, Min(0.1f)] private float playerTurnAssistFalloffSpeed = 18f;
        [SerializeField, Min(0f)] private float playerDriftYawAssist = 3f;

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
        private float activePathLength = 0.1f;
        private int currentEdgeIndex = -1;
        private int pendingEdgeIndex = -1;
        private bool isTurning;
        private bool routeInitialized;
        private bool isPlayerControlled;
        private bool initialNpcControllerEnabled;
        private bool initialObstacleSensorEnabled;
        private bool initialKinematicState;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private float playerSteerInput;
        private float playerThrottleInput;
        private bool playerHandbrakeInput;
        private bool hasCachedRearGrip;
        private float rearGripBlend;
        private WheelFrictionCurve rearLeftBaseSidewaysFriction;
        private WheelFrictionCurve rearRightBaseSidewaysFriction;
        private float currentSteerAngle;
        private float nextRoadDetectionTime;
        private Vector3 lastTargetPoint;
        private TrafficCarSpawner trafficSpawnerOwner;
        private AudioSource engineAudioSource;

        public bool IsPlayerControlled => isPlayerControlled;
        public bool IsDecorativeVehicle => isDecorativeVehicle;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            obstacleSensor = obstacleSensor != null ? obstacleSensor : GetComponent<VehicleObstacleSensor>();
            initialNpcControllerEnabled = enabled;
            initialObstacleSensorEnabled = obstacleSensor != null && obstacleSensor.enabled;
            initialKinematicState = body != null && body.isKinematic;
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
            if (engineAudioSource != null && engineAudioSource.isPlaying)
            {
                engineAudioSource.Stop();
            }
        }

        public void InitializeTrafficRoute(TrafficNetwork network, int edgeIndex)
        {
            trafficNetwork = network;
            currentEdgeIndex = edgeIndex;
            pendingEdgeIndex = -1;
            isTurning = false;
            isPlayerControlled = false;
            playerSteerInput = 0f;
            playerThrottleInput = 0f;
            playerHandbrakeInput = false;
            if (body != null)
            {
                body.isKinematic = false;
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.WakeUp();
            }
            if (obstacleSensor != null) obstacleSensor.enabled = true;
            routeInitialized = trafficNetwork != null && trafficNetwork.IsValidEdgeIndex(edgeIndex);
            if (routeInitialized)
            {
                SetActivePath(trafficNetwork.GetRoadPath(currentEdgeIndex));
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

        public void SetTrafficSpawnerOwner(TrafficCarSpawner spawner)
        {
            trafficSpawnerOwner = spawner;
        }

        public void BeginPlayerControl()
        {
            enabled = true;
            isPlayerControlled = true;
            disableNpcDriving = false;
            routeInitialized = false;
            pendingEdgeIndex = -1;
            isTurning = false;
            playerSteerInput = 0f;
            playerThrottleInput = 0f;
            playerHandbrakeInput = false;
            CacheRearGrip();
            rearGripBlend = 0f;
            if (obstacleSensor != null) obstacleSensor.enabled = false;
            if (body != null)
            {
                body.isKinematic = false;
                body.WakeUp();
            }
        }

        public void SetPlayerDrivingInput(float steering, float throttle, bool handbrake)
        {
            playerSteerInput = Mathf.Clamp(steering, -1f, 1f);
            playerThrottleInput = Mathf.Clamp(throttle, -1f, 1f);
            playerHandbrakeInput = handbrake;
        }

        public void ParkAfterPlayerExit()
        {
            isPlayerControlled = false;
            routeInitialized = false;
            playerSteerInput = 0f;
            playerThrottleInput = 0f;
            playerHandbrakeInput = false;
            currentSteerAngle = 0f;
            RestoreRearGrip();
            ApplyDrive(0f, playerHandbrakeTorque, 0f);
            if (body != null)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
            }
            if (obstacleSensor != null) obstacleSensor.enabled = false;
            enabled = false;
        }

        public void RestoreDecorativeState()
        {
            isPlayerControlled = false;
            routeInitialized = false;
            playerSteerInput = 0f;
            playerThrottleInput = 0f;
            playerHandbrakeInput = false;
            RestoreRearGrip();
            ApplyDrive(0f, playerHandbrakeTorque, 0f);
            if (body != null)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = initialKinematicState;
                body.position = initialPosition;
                body.rotation = initialRotation;
            }
            else
            {
                transform.SetPositionAndRotation(initialPosition, initialRotation);
            }
            Physics.SyncTransforms();
            if (obstacleSensor != null) obstacleSensor.enabled = initialObstacleSensorEnabled;
            enabled = initialNpcControllerEnabled;
        }

        public bool TryReturnToTrafficPool()
        {
            return trafficSpawnerOwner != null && trafficSpawnerOwner.TryRecyclePlayerVehicle(this);
        }

        private void FixedUpdate()
        {
            if (isPlayerControlled)
            {
                DrivePlayerControlled();
                return;
            }

            if (isDecorativeVehicle)
            {
                return;
            }

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

            float pathLength = activePathLength;
            float closestT = activePath.FindClosestT(transform.position, pathSamples);
            // Change to the intersection curve before reaching the end of the road segment.
            // Waiting until the bumper reaches the end makes a WheelCollider vehicle drive straight through the junction.
            float turnPreparationT = Mathf.Clamp01(1f - lookAheadDistance / pathLength);
            if (closestT >= turnPreparationT)
            {
                AdvanceRoute();
                pathLength = activePathLength;
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

        private void DrivePlayerControlled()
        {
            float forwardSpeed = body != null ? Vector3.Dot(body.velocity, transform.forward) : 0f;
            float speedFactor = Mathf.InverseLerp(0f, playerSteerReductionSpeed, Mathf.Abs(forwardSpeed));
            float maxPlayerSteer = Mathf.Lerp(playerMaxSteerAngle, playerHighSpeedSteerAngle, speedFactor);
            currentSteerAngle = Mathf.MoveTowards(
                currentSteerAngle,
                playerSteerInput * maxPlayerSteer,
                playerSteerResponse * Time.fixedDeltaTime);

            float motorTorque = 0f;
            float brakeTorque = 0f;

            if (playerThrottleInput > 0.01f)
            {
                if (forwardSpeed < playerMaxForwardSpeed)
                {
                    float speedRatio = Mathf.Clamp01(Mathf.Max(0f, forwardSpeed) / playerMaxForwardSpeed);
                    float falloffT = Mathf.InverseLerp(playerAccelerationFalloffStart, 1f, speedRatio);
                    float accelerationFactor = Mathf.Lerp(1f, playerTopSpeedAccelerationFactor, falloffT);
                    motorTorque = playerThrottleInput * playerMotorTorque * accelerationFactor;
                    float launchAssist = 1f - Mathf.InverseLerp(2f, 7f, Mathf.Max(0f, forwardSpeed));
                    body.AddForce(transform.forward * playerThrottleInput * playerArcadeAcceleration * launchAssist,
                        ForceMode.Acceleration);
                }
            }
            else if (playerThrottleInput < -0.01f)
            {
                if (forwardSpeed > 0.5f)
                {
                    brakeTorque = Mathf.Max(brakeTorque, playerBrakeTorque);
                    body.AddForce(-transform.forward * playerArcadeBrakeDeceleration, ForceMode.Acceleration);
                }
                else if (forwardSpeed > -playerMaxReverseSpeed)
                {
                    motorTorque = playerThrottleInput * playerReverseTorque;
                    body.AddForce(-transform.forward * playerArcadeReverseAcceleration, ForceMode.Acceleration);
                }
            }

            bool isDrifting = playerHandbrakeInput && Mathf.Abs(forwardSpeed) > 2f && Mathf.Abs(playerSteerInput) > 0.1f;
            rearGripBlend = Mathf.MoveTowards(rearGripBlend, isDrifting ? 1f : 0f, playerGripChangeSpeed * Time.fixedDeltaTime);
            ApplyRearGrip(rearGripBlend);

            if (body != null)
            {
                float lateralSpeed = Vector3.Dot(body.velocity, transform.right);
                if (isDrifting)
                {
                    float turnDirection = playerSteerInput * Mathf.Sign(forwardSpeed);
                    body.AddTorque(Vector3.up * turnDirection * playerDriftYawAssist, ForceMode.Acceleration);
                }
                else
                {
                    float turnDirection = playerSteerInput * Mathf.Sign(forwardSpeed);
                    float speedAssist = 1f - Mathf.InverseLerp(
                        playerTurnAssistFalloffSpeed * 0.65f,
                        playerTurnAssistFalloffSpeed,
                        Mathf.Abs(forwardSpeed));
                    float yawAssist = playerNormalYawAssist * 0.35f * speedAssist;
                    body.AddTorque(Vector3.up * turnDirection * yawAssist, ForceMode.Acceleration);
                    body.AddForce(-transform.right * lateralSpeed * playerLateralGripAssist, ForceMode.Acceleration);
                }
            }

            ApplyPlayerDrive(motorTorque, brakeTorque, currentSteerAngle, playerHandbrakeInput);
        }

        private void ApplyPlayerDrive(float motorTorque, float brakeTorque, float steerAngle, bool handbrake)
        {
            ApplySteer(frontLeftWheel, steerAngle);
            ApplySteer(frontRightWheel, steerAngle);
            ApplyMotor(frontLeftWheel, frontWheelDrive ? motorTorque : 0f);
            ApplyMotor(frontRightWheel, frontWheelDrive ? motorTorque : 0f);
            ApplyMotor(rearLeftWheel, rearWheelDrive ? motorTorque : 0f);
            ApplyMotor(rearRightWheel, rearWheelDrive ? motorTorque : 0f);
            ApplyBrake(frontLeftWheel, brakeTorque);
            ApplyBrake(frontRightWheel, brakeTorque);
            float rearBrakeTorque = handbrake ? Mathf.Max(brakeTorque, playerHandbrakeTorque) : brakeTorque;
            ApplyBrake(rearLeftWheel, rearBrakeTorque);
            ApplyBrake(rearRightWheel, rearBrakeTorque);
        }

        private void CacheRearGrip()
        {
            if (rearLeftWheel != null) rearLeftBaseSidewaysFriction = rearLeftWheel.sidewaysFriction;
            if (rearRightWheel != null) rearRightBaseSidewaysFriction = rearRightWheel.sidewaysFriction;
            hasCachedRearGrip = rearLeftWheel != null || rearRightWheel != null;
        }

        private void ApplyRearGrip(float driftBlend)
        {
            if (!hasCachedRearGrip)
            {
                return;
            }

            ApplyWheelSidewaysGrip(rearLeftWheel, rearLeftBaseSidewaysFriction, driftBlend);
            ApplyWheelSidewaysGrip(rearRightWheel, rearRightBaseSidewaysFriction, driftBlend);
        }

        private void RestoreRearGrip()
        {
            if (!hasCachedRearGrip)
            {
                return;
            }

            if (rearLeftWheel != null) rearLeftWheel.sidewaysFriction = rearLeftBaseSidewaysFriction;
            if (rearRightWheel != null) rearRightWheel.sidewaysFriction = rearRightBaseSidewaysFriction;
            rearGripBlend = 0f;
        }

        private void ApplyWheelSidewaysGrip(WheelCollider wheel, WheelFrictionCurve baseFriction, float driftBlend)
        {
            if (wheel == null)
            {
                return;
            }

            WheelFrictionCurve friction = baseFriction;
            friction.stiffness = baseFriction.stiffness * Mathf.Lerp(1f, playerDriftRearGrip, driftBlend);
            wheel.sidewaysFriction = friction;
        }

        private void ResolveNetworkAndRoute()
        {
            if (trafficNetwork == null) trafficNetwork = TrafficNetwork.Active;
            if (routeInitialized || trafficNetwork == null) return;
            if (!autoDetectStartingRoad) return;
            if (Time.time < nextRoadDetectionTime) return;

            nextRoadDetectionTime = Time.time + RoadDetectionRetryDelay;

            if (trafficNetwork.TryFindClosestEdge(transform.position, transform.forward, maxStartingRoadDetectionDistance, out int edgeIndex))
            {
                currentEdgeIndex = edgeIndex;
                SetActivePath(trafficNetwork.GetRoadPath(edgeIndex));
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

                SetActivePath(trafficNetwork.GetTurnPath(currentEdgeIndex, pendingEdgeIndex));
                isTurning = true;
                return;
            }

            currentEdgeIndex = pendingEdgeIndex;
            pendingEdgeIndex = -1;
            SetActivePath(trafficNetwork.GetRoadPath(currentEdgeIndex));
            isTurning = false;
        }

        private void SetActivePath(TrafficPath path)
        {
            activePath = path;
            activePathLength = Mathf.Max(0.1f, path.Length);
        }

        private void LateUpdate()
        {
            UpdateWheelVisual(frontLeftWheel, frontLeftVisual);
            UpdateWheelVisual(frontRightWheel, frontRightVisual);
            UpdateWheelVisual(rearLeftWheel, rearLeftVisual);
            UpdateWheelVisual(rearRightWheel, rearRightVisual);
            UpdateEngineAudio();
        }

        private void UpdateEngineAudio()
        {
            TrafficAudioSystem audioSystem = TrafficAudioSystem.Active;
            if (audioSystem == null)
            {
                return;
            }

            if (engineAudioSource == null)
            {
                engineAudioSource = audioSystem.CreateEngineSource(gameObject);
            }

            float speed = body != null ? body.velocity.magnitude : 0f;
            audioSystem.UpdateEngineSource(engineAudioSource, speed, !isDecorativeVehicle && speed > 0.1f);
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
