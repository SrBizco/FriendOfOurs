using FriendOfOurs.Gameplay;
using UnityEngine;

namespace FriendOfOurs.NPCs
{
    [RequireComponent(typeof(PedestrianController))]
    public sealed class PoliceOfficerController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float detectionDistance = 32f;

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

            // Once the player is wanted, nearby officers immediately engage.
            // Requiring an unobstructed ray caused officers behind street props
            // or other pedestrians to keep behaving like civilians.
            if (!pursuingPlayer && IsPlayerWithinDetectionDistance())
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

        private bool IsPlayerWithinDetectionDistance()
        {
            Vector3 offset = player.position - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= detectionDistance * detectionDistance;
        }
    }
}
