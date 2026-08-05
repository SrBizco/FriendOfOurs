using UnityEngine;

namespace FriendOfOurs.Gameplay
{
    /// <summary>
    /// Lets the player possess and move any nearby object that has a collider on the configured prop layers.
    /// No component is required on the prop itself.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class GhostController : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private KeyCode possessKey = KeyCode.F;
        [SerializeField] private KeyCode releaseKey = KeyCode.G;
        [SerializeField, Min(0.1f)] private float interactionRadius = 2.5f;
        [SerializeField] private LayerMask propLayers;

        [Header("Possessed prop movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 4f;
        [SerializeField, Min(0f)] private float rotationSpeed = 540f;

        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerCombatController combatController;
        [SerializeField] private PlayerAnimationController animationController;
        [SerializeField] private PlayerVehicleController vehicleController;
        [SerializeField] private TopDownCameraController cameraController;

        private readonly Collider[] nearbyColliders = new Collider[24];
        private Rigidbody playerBody;
        private Renderer[] playerRenderers;
        private Collider[] playerColliders;
        private bool[] rendererEnabledStates;
        private bool[] colliderEnabledStates;
        private bool playerControllerWasEnabled;
        private bool combatControllerWasEnabled;
        private bool animationControllerWasEnabled;
        private bool vehicleControllerWasEnabled;
        private bool playerBodyWasKinematic;
        private bool playerBodyUsedGravity;
        private RigidbodyConstraints playerBodyConstraints;

        private Transform possessedTransform;
        private GameObject possessedClone;
        private Rigidbody possessedBody;
        private Collider[] sourceColliders;
        private bool[] sourceColliderEnabledStates;

        public bool IsPossessing => possessedTransform != null;

        private void Awake()
        {
            playerBody = GetComponent<Rigidbody>();
            if (playerController == null) playerController = GetComponent<PlayerController>();
            if (combatController == null) combatController = GetComponent<PlayerCombatController>();
            if (animationController == null) animationController = GetComponentInChildren<PlayerAnimationController>();
            if (vehicleController == null) vehicleController = GetComponent<PlayerVehicleController>();
            if (cameraController == null && Camera.main != null)
            {
                cameraController = Camera.main.GetComponent<TopDownCameraController>();
            }

            playerRenderers = GetComponentsInChildren<Renderer>(true);
            playerColliders = GetComponentsInChildren<Collider>(true);
            rendererEnabledStates = new bool[playerRenderers.Length];
            colliderEnabledStates = new bool[playerColliders.Length];
        }

        private void Update()
        {
            if (possessedTransform == null)
            {
                if (Input.GetKeyDown(possessKey) && (vehicleController == null || !vehicleController.IsDriving))
                {
                    TryPossessNearestProp();
                }

                return;
            }

            if (Input.GetKeyDown(releaseKey))
            {
                ReleaseProp();
            }
        }

        private void FixedUpdate()
        {
            if (possessedTransform == null || possessedBody == null)
            {
                return;
            }

            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            Transform referenceTransform = Camera.main != null ? Camera.main.transform : null;
            Vector3 direction = referenceTransform != null
                ? TopDownControlMath.GetCameraRelativeMoveDirection(input, referenceTransform.forward)
                : TopDownControlMath.GetWorldMoveDirection(input);

            if (direction.sqrMagnitude > 0.0001f)
            {
                MovePossessedProp(direction.normalized);
            }

            playerBody.MovePosition(possessedBody.position);
            playerBody.MoveRotation(possessedBody.rotation);
        }

        private void TryPossessNearestProp()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                interactionRadius,
                nearbyColliders,
                propLayers,
                QueryTriggerInteraction.Ignore);

            Transform closestProp = null;
            float closestSqrDistance = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = nearbyColliders[i];
                if (hit == null)
                {
                    continue;
                }

                Transform candidate = GetPossessableRoot(hit.transform);
                if (candidate == transform)
                {
                    continue;
                }

                float sqrDistance = (candidate.position - transform.position).sqrMagnitude;
                if (sqrDistance < closestSqrDistance)
                {
                    closestSqrDistance = sqrDistance;
                    closestProp = candidate;
                }
            }

            if (closestProp == null)
            {
                return;
            }

            possessedClone = Instantiate(
                closestProp.gameObject,
                closestProp.position,
                closestProp.rotation);
            possessedClone.name = closestProp.name + " (Ghost Copy)";
            SetStaticRecursively(possessedClone.transform, false);
            DisableSourceColliders(closestProp);

            possessedTransform = possessedClone.transform;
            possessedBody = possessedTransform.GetComponent<Rigidbody>();
            if (possessedBody == null)
            {
                possessedBody = possessedTransform.gameObject.AddComponent<Rigidbody>();
            }

            possessedBody.velocity = Vector3.zero;
            possessedBody.angularVelocity = Vector3.zero;
            possessedBody.isKinematic = false;
            possessedBody.useGravity = false;
            possessedBody.constraints = RigidbodyConstraints.FreezePositionY
                | RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezeRotationZ;
            possessedBody.interpolation = RigidbodyInterpolation.Interpolate;
            possessedBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            SetPlayerPossessionState(true);
            if (cameraController != null)
            {
                cameraController.Target = possessedTransform;
            }
        }

        private void MovePossessedProp(Vector3 direction)
        {
            float moveDistance = moveSpeed * Time.fixedDeltaTime;
            possessedBody.MovePosition(possessedBody.position + direction * moveDistance);

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            Quaternion nextRotation = Quaternion.RotateTowards(
                possessedBody.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime);
            possessedBody.MoveRotation(nextRotation);
        }

        private void ReleaseProp()
        {
            Vector3 releasePosition = possessedBody.position;
            Quaternion releaseRotation = possessedBody.rotation;

            if (possessedClone != null)
            {
                Collider[] cloneColliders = possessedClone.GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < cloneColliders.Length; i++)
                {
                    cloneColliders[i].enabled = false;
                }

                Destroy(possessedClone);
            }

            RestoreSourceColliders();

            possessedTransform = null;
            possessedClone = null;
            possessedBody = null;

            playerBody.position = releasePosition;
            playerBody.rotation = releaseRotation;
            SetPlayerPossessionState(false);
            if (cameraController != null)
            {
                cameraController.Target = transform;
            }
        }

        private Transform GetPossessableRoot(Transform colliderTransform)
        {
            Transform root = colliderTransform;
            while (root.parent != null && root.parent.gameObject.layer == colliderTransform.gameObject.layer)
            {
                root = root.parent;
            }

            return root;
        }

        private static void SetStaticRecursively(Transform root, bool isStatic)
        {
            root.gameObject.isStatic = isStatic;
            for (int i = 0; i < root.childCount; i++)
            {
                SetStaticRecursively(root.GetChild(i), isStatic);
            }
        }

        private void DisableSourceColliders(Transform source)
        {
            sourceColliders = source.GetComponentsInChildren<Collider>(true);
            sourceColliderEnabledStates = new bool[sourceColliders.Length];
            for (int i = 0; i < sourceColliders.Length; i++)
            {
                if (sourceColliders[i] == null)
                {
                    continue;
                }

                sourceColliderEnabledStates[i] = sourceColliders[i].enabled;
                sourceColliders[i].enabled = false;
            }
        }

        private void RestoreSourceColliders()
        {
            if (sourceColliders == null || sourceColliderEnabledStates == null)
            {
                return;
            }

            for (int i = 0; i < sourceColliders.Length; i++)
            {
                if (sourceColliders[i] != null)
                {
                    sourceColliders[i].enabled = sourceColliderEnabledStates[i];
                }
            }

            sourceColliders = null;
            sourceColliderEnabledStates = null;
        }

        private void SetPlayerPossessionState(bool isPossessing)
        {
            if (isPossessing)
            {
                playerControllerWasEnabled = playerController != null && playerController.enabled;
                combatControllerWasEnabled = combatController != null && combatController.enabled;
                animationControllerWasEnabled = animationController != null && animationController.enabled;
                vehicleControllerWasEnabled = vehicleController != null && vehicleController.enabled;
                playerBodyWasKinematic = playerBody.isKinematic;
                playerBodyUsedGravity = playerBody.useGravity;
                playerBodyConstraints = playerBody.constraints;
                playerController?.StopMovement(false);

                if (playerController != null) playerController.enabled = false;
                if (combatController != null) combatController.enabled = false;
                if (animationController != null) animationController.enabled = false;
                if (vehicleController != null) vehicleController.enabled = false;

                for (int i = 0; i < playerRenderers.Length; i++)
                {
                    rendererEnabledStates[i] = playerRenderers[i].enabled;
                    playerRenderers[i].enabled = false;
                }

                for (int i = 0; i < playerColliders.Length; i++)
                {
                    colliderEnabledStates[i] = playerColliders[i].enabled;
                    playerColliders[i].enabled = false;
                }

                playerBody.velocity = Vector3.zero;
                playerBody.angularVelocity = Vector3.zero;
                playerBody.isKinematic = true;
                playerBody.useGravity = false;
                return;
            }

            for (int i = 0; i < playerRenderers.Length; i++)
            {
                playerRenderers[i].enabled = rendererEnabledStates[i];
            }

            for (int i = 0; i < playerColliders.Length; i++)
            {
                playerColliders[i].enabled = colliderEnabledStates[i];
            }

            playerBody.isKinematic = playerBodyWasKinematic;
            playerBody.useGravity = playerBodyUsedGravity;
            playerBody.constraints = playerBodyConstraints;
            playerBody.velocity = Vector3.zero;
            playerBody.angularVelocity = Vector3.zero;
            if (playerController != null) playerController.enabled = playerControllerWasEnabled;
            if (combatController != null) combatController.enabled = combatControllerWasEnabled;
            if (animationController != null) animationController.enabled = animationControllerWasEnabled;
            if (vehicleController != null) vehicleController.enabled = vehicleControllerWasEnabled;
        }
    }
}
