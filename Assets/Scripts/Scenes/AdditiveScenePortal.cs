using FriendOfOurs.Gameplay;
using UnityEngine;

namespace FriendOfOurs.Scenes
{
    [RequireComponent(typeof(Collider))]
    public sealed class AdditiveScenePortal : MonoBehaviour
    {
        [Header("Destination")]
        [SerializeField] private string destinationScene;
        [SerializeField] private string destinationSpawnId;
        [SerializeField] private bool unloadCurrentSceneAfterArrival;

        [Header("Interaction")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        private PlayerController nearbyPlayer;

        private void Reset()
        {
            Collider portalCollider = GetComponent<Collider>();
            if (portalCollider != null)
            {
                portalCollider.isTrigger = true;
            }
        }

        private void Update()
        {
            if (nearbyPlayer != null && Input.GetKeyDown(interactKey))
            {
                SceneTransitionSystem.RequestTransition(this, nearbyPlayer);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                nearbyPlayer = player;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null && player == nearbyPlayer)
            {
                nearbyPlayer = null;
            }
        }

        public string DestinationScene => destinationScene;
        public string DestinationSpawnId => destinationSpawnId;
        public bool UnloadCurrentSceneAfterArrival => unloadCurrentSceneAfterArrival;
    }
}
