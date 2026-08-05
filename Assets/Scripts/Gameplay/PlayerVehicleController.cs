using System.Collections.Generic;
using FriendOfOurs.Traffic;
using UnityEngine;

namespace FriendOfOurs.Gameplay
{
    /// <summary>Lets the player enter, drive and leave one WheelCollider vehicle at a time.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerVehicleController : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField, Min(0.1f)] private float interactionRadius = 3f;
        [SerializeField, Min(0.1f)] private float leftDoorDistance = 2.2f;
        [SerializeField] private LayerMask vehicleLayers = Physics.DefaultRaycastLayers;

        [Header("Abandoned vehicles")]
        [SerializeField, Min(1f)] private float visibleRecycleDelay = 120f;
        [SerializeField, Min(1f)] private float immediateRecycleDistance = 90f;
        [SerializeField, Range(0f, 0.5f)] private float cameraMargin = 0.08f;

        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerCombatController combatController;
        [SerializeField] private TopDownCameraController cameraController;

        private readonly Collider[] nearbyColliders = new Collider[24];
        private Rigidbody playerBody;
        private Renderer[] playerRenderers;
        private Collider[] playerColliders;
        private bool[] rendererEnabledStates;
        private bool[] colliderEnabledStates;
        private bool playerControllerWasEnabled;
        private bool combatControllerWasEnabled;
        private bool playerBodyWasKinematic;
        private NpcCarController activeVehicle;
        private readonly List<AbandonedVehicle> abandonedVehicles = new List<AbandonedVehicle>();

        public bool IsDriving => activeVehicle != null;

        private sealed class AbandonedVehicle
        {
            public NpcCarController Vehicle;
            public float RecycleTime;
        }

        private void Awake()
        {
            playerBody = GetComponent<Rigidbody>();
            if (playerController == null) playerController = GetComponent<PlayerController>();
            if (combatController == null) combatController = GetComponent<PlayerCombatController>();
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
            RecycleAbandonedVehicles();
            if (!Input.GetKeyDown(interactKey))
            {
                return;
            }

            if (activeVehicle != null)
            {
                ExitVehicle();
            }
            else
            {
                TryEnterNearestVehicle();
            }
        }

        private void FixedUpdate()
        {
            if (activeVehicle == null)
            {
                return;
            }

            activeVehicle.SetPlayerDrivingInput(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical"),
                Input.GetKey(KeyCode.Space));
            playerBody.MovePosition(activeVehicle.transform.position);
            playerBody.MoveRotation(activeVehicle.transform.rotation);
        }

        private void TryEnterNearestVehicle()
        {
            NpcCarController vehicle = FindNearestVehicle();
            if (vehicle == null)
            {
                return;
            }

            CancelAbandonedVehicle(vehicle);
            activeVehicle = vehicle;
            SetPlayerDrivingState(true);
            activeVehicle.BeginPlayerControl();
            cameraController?.SetVehicleFollow(activeVehicle.transform);
        }

        private void ExitVehicle()
        {
            NpcCarController vehicle = activeVehicle;
            activeVehicle = null;
            vehicle.ParkAfterPlayerExit();

            Vector3 exitPosition = vehicle.transform.position - vehicle.transform.right * leftDoorDistance + Vector3.up * 0.1f;
            SetPlayerDrivingState(false);
            playerBody.position = exitPosition;
            playerBody.rotation = Quaternion.LookRotation(-vehicle.transform.right, Vector3.up);
            cameraController?.ClearVehicleFollow(transform);
            ScheduleAbandonedVehicle(vehicle);
        }

        private NpcCarController FindNearestVehicle()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                interactionRadius,
                nearbyColliders,
                vehicleLayers,
                QueryTriggerInteraction.Ignore);

            NpcCarController closestVehicle = null;
            float closestSqrDistance = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = nearbyColliders[i];
                NpcCarController vehicle = hit != null ? hit.GetComponentInParent<NpcCarController>() : null;
                if (vehicle == null || vehicle.IsPlayerControlled)
                {
                    continue;
                }

                Vector3 doorPosition = vehicle.transform.position - vehicle.transform.right * leftDoorDistance;
                float sqrDistance = (transform.position - doorPosition).sqrMagnitude;
                if (sqrDistance < closestSqrDistance)
                {
                    closestSqrDistance = sqrDistance;
                    closestVehicle = vehicle;
                }
            }

            return closestVehicle;
        }

        private void ScheduleAbandonedVehicle(NpcCarController vehicle)
        {
            CancelAbandonedVehicle(vehicle);
            abandonedVehicles.Add(new AbandonedVehicle
            {
                Vehicle = vehicle,
                RecycleTime = Time.time + visibleRecycleDelay
            });
        }

        private void CancelAbandonedVehicle(NpcCarController vehicle)
        {
            for (int i = abandonedVehicles.Count - 1; i >= 0; i--)
            {
                if (abandonedVehicles[i].Vehicle == vehicle)
                {
                    abandonedVehicles.RemoveAt(i);
                }
            }
        }

        private void RecycleAbandonedVehicles()
        {
            float squaredRecycleDistance = immediateRecycleDistance * immediateRecycleDistance;
            for (int i = abandonedVehicles.Count - 1; i >= 0; i--)
            {
                AbandonedVehicle abandoned = abandonedVehicles[i];
                NpcCarController vehicle = abandoned.Vehicle;
                if (vehicle == null || !vehicle.gameObject.activeSelf)
                {
                    abandonedVehicles.RemoveAt(i);
                    continue;
                }

                bool isFarAndHidden = (vehicle.transform.position - transform.position).sqrMagnitude > squaredRecycleDistance
                    && !IsVisible(vehicle.transform.position);
                if (!isFarAndHidden && Time.time < abandoned.RecycleTime)
                {
                    continue;
                }

                if (vehicle.IsDecorativeVehicle)
                {
                    vehicle.RestoreDecorativeState();
                }
                else
                {
                    vehicle.TryReturnToTrafficPool();
                }

                abandonedVehicles.RemoveAt(i);
            }
        }

        private bool IsVisible(Vector3 position)
        {
            Camera targetCamera = Camera.main;
            if (targetCamera == null)
            {
                return false;
            }

            Vector3 viewport = targetCamera.WorldToViewportPoint(position);
            return viewport.z > 0f
                && viewport.x >= -cameraMargin && viewport.x <= 1f + cameraMargin
                && viewport.y >= -cameraMargin && viewport.y <= 1f + cameraMargin;
        }

        private void SetPlayerDrivingState(bool isDriving)
        {
            if (isDriving)
            {
                playerControllerWasEnabled = playerController != null && playerController.enabled;
                combatControllerWasEnabled = combatController != null && combatController.enabled;
                playerBodyWasKinematic = playerBody.isKinematic;
                playerController?.StopMovement(false);
                if (playerController != null) playerController.enabled = false;
                if (combatController != null) combatController.enabled = false;

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
            playerBody.velocity = Vector3.zero;
            playerBody.angularVelocity = Vector3.zero;
            if (playerController != null) playerController.enabled = playerControllerWasEnabled;
            if (combatController != null) combatController.enabled = combatControllerWasEnabled;
        }
    }
}
