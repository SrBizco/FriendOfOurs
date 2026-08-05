using FriendOfOurs.Gameplay;
using UnityEngine;

namespace FriendOfOurs.NPCs
{
    [RequireComponent(typeof(PedestrianController))]
    public sealed class PoliceOfficerController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float detectionDistance = 16f;
        [SerializeField, Min(0f)] private float eyeHeight = 1.2f;
        [SerializeField] private LayerMask visionLayers = Physics.DefaultRaycastLayers;

        private PedestrianController pedestrian;
        private Transform player;
        private WantedSystem wantedSystem;
        private bool pursuingPlayer;

        public PedestrianController Pedestrian => pedestrian;

        private void Awake()
        {
            pedestrian = GetComponent<PedestrianController>();
        }

        private void Update()
        {
            if (pedestrian == null || pedestrian.IsDead || player == null || wantedSystem == null)
            {
                return;
            }

            if (!wantedSystem.IsWanted)
            {
                ReleasePursuit();
                return;
            }

            if (!pursuingPlayer && CanSeePlayer())
            {
                pursuingPlayer = true;
                pedestrian.SetPersistentCombatTarget(player);
            }
        }

        public void Initialize(Transform targetPlayer, WantedSystem targetWantedSystem)
        {
            player = targetPlayer;
            wantedSystem = targetWantedSystem;
            pursuingPlayer = false;
            pedestrian ??= GetComponent<PedestrianController>();
            pedestrian?.ReleasePersistentCombatTarget();
        }

        public void ReleasePursuit()
        {
            if (!pursuingPlayer)
            {
                return;
            }

            pursuingPlayer = false;
            pedestrian?.ReleasePersistentCombatTarget();
        }

        private bool CanSeePlayer()
        {
            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            Vector3 target = player.position + Vector3.up * eyeHeight;
            Vector3 direction = target - origin;
            float distance = direction.magnitude;
            if (distance > detectionDistance)
            {
                return false;
            }

            if (distance <= 0.01f)
            {
                return true;
            }

            if (!Physics.Raycast(origin, direction / distance, out RaycastHit hit, distance, visionLayers, QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            return hit.collider != null &&
                   (hit.collider.transform == player || hit.collider.transform.IsChildOf(player));
        }
    }
}
