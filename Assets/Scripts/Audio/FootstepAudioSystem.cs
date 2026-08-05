using FriendOfOurs.Gameplay;
using UnityEngine;
using UnityEngine.Audio;

namespace FriendOfOurs.Audio
{
    public sealed class FootstepAudioSystem : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerVehicleController vehicleController;
        [SerializeField] private GhostController ghostController;

        [Header("Clips")]
        [SerializeField] private AudioClip[] normalFootsteps;
        [SerializeField] private AudioClip[] dirtFootsteps;
        [SerializeField] private AudioMixerGroup outputMixerGroup;

        [Header("Step detection")]
        [SerializeField, Min(0.1f)] private float stepDistance = 1.7f;
        [SerializeField, Min(0.1f)] private float raycastHeight = 1.2f;
        [SerializeField, Min(0.1f)] private float raycastDistance = 2f;
        [SerializeField] private LayerMask surfaceLayers = Physics.DefaultRaycastLayers;
        [SerializeField] private LayerMask dirtLayers;

        [Header("Audio")]
        [SerializeField, Range(0f, 1f)] private float volume = 0.65f;
        [SerializeField, Min(0.1f)] private float minimumDistance = 1f;
        [SerializeField, Min(1f)] private float maximumDistance = 12f;

        private readonly RaycastHit[] groundHits = new RaycastHit[8];
        private AudioSource footstepSource;
        private Vector3 lastPosition;
        private float distanceSinceLastStep;

        private void Awake()
        {
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
            }

            if (player != null)
            {
                if (vehicleController == null) vehicleController = player.GetComponent<PlayerVehicleController>();
                if (ghostController == null) ghostController = player.GetComponent<GhostController>();
                lastPosition = player.transform.position;
            }

            footstepSource = GetComponent<AudioSource>();
            if (footstepSource == null)
            {
                footstepSource = gameObject.AddComponent<AudioSource>();
            }
            footstepSource.playOnAwake = false;
            // Footsteps are feedback for the local player. In a top-down camera
            // their source is far below the AudioListener, so 3D attenuation
            // would make them nearly inaudible.
            footstepSource.spatialBlend = 0f;
            footstepSource.minDistance = minimumDistance;
            footstepSource.maxDistance = maximumDistance;
            footstepSource.outputAudioMixerGroup = outputMixerGroup;
        }

        private void Update()
        {
            if (player == null)
            {
                return;
            }

            Vector3 currentPosition = player.transform.position;
            Vector3 horizontalDelta = currentPosition - lastPosition;
            horizontalDelta.y = 0f;
            lastPosition = currentPosition;

            if (!CanPlayFootsteps())
            {
                distanceSinceLastStep = 0f;
                return;
            }

            distanceSinceLastStep += horizontalDelta.magnitude;
            if (distanceSinceLastStep < stepDistance)
            {
                return;
            }

            if (TryGetGroundHit(out RaycastHit groundHit))
            {
                PlayFootstep(groundHit);
            }

            distanceSinceLastStep = 0f;
        }

        private bool CanPlayFootsteps()
        {
            if (player.IsDead || (vehicleController != null && vehicleController.IsDriving) ||
                (ghostController != null && ghostController.IsPossessing))
            {
                return false;
            }

            return true;
        }

        private bool TryGetGroundHit(out RaycastHit groundHit)
        {
            Vector3 origin = player.transform.position + Vector3.up * raycastHeight;
            int hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                groundHits,
                raycastDistance,
                surfaceLayers | dirtLayers,
                QueryTriggerInteraction.Ignore);

            int closestHitIndex = -1;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = groundHits[i];
                if (hit.collider == null || hit.collider.transform.IsChildOf(player.transform))
                {
                    continue;
                }

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    closestHitIndex = i;
                }
            }

            if (closestHitIndex >= 0)
            {
                groundHit = groundHits[closestHitIndex];
                return true;
            }

            groundHit = default;
            return false;
        }

        private void PlayFootstep(RaycastHit groundHit)
        {
            AudioClip[] clips = IsDirtLayer(groundHit.collider.gameObject.layer) ? dirtFootsteps : normalFootsteps;
            if (clips == null || clips.Length == 0 || footstepSource == null)
            {
                return;
            }

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip == null)
            {
                return;
            }

            footstepSource.transform.position = groundHit.point;
            footstepSource.PlayOneShot(clip, volume);
        }

        private bool IsDirtLayer(int layer)
        {
            return (dirtLayers.value & (1 << layer)) != 0;
        }
    }
}
