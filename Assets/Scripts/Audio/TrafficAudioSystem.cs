using UnityEngine;
using UnityEngine.Audio;

namespace FriendOfOurs.Audio
{
    public sealed class TrafficAudioSystem : MonoBehaviour
    {
        public static TrafficAudioSystem Active { get; private set; }

        [Header("Engine loop")]
        [SerializeField] private AudioClip engineLoop;
        [SerializeField] private AudioMixerGroup outputMixerGroup;
        [SerializeField, Range(0f, 1f)] private float idleVolume = 0.08f;
        [SerializeField, Range(0f, 1f)] private float maxVolume = 0.55f;
        [SerializeField, Min(0.1f)] private float speedForMaxPitch = 16f;
        [SerializeField, Min(0.1f)] private float minimumDistance = 4f;
        [SerializeField, Min(1f)] private float maximumDistance = 45f;
        [SerializeField, Range(0f, 1f)] private float spatialBlend = 0.55f;
        [SerializeField, Range(0.1f, 3f)] private float idlePitch = 0.75f;
        [SerializeField, Range(0.1f, 3f)] private float maxPitch = 1.35f;

        private void Awake()
        {
            Active = this;
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        public AudioSource CreateEngineSource(GameObject vehicle)
        {
            if (vehicle == null || engineLoop == null)
            {
                return null;
            }

            AudioSource source = vehicle.GetComponent<AudioSource>();
            if (source == null)
            {
                source = vehicle.AddComponent<AudioSource>();
            }

            source.clip = engineLoop;
            source.loop = true;
            source.playOnAwake = false;
            // Keep the sound located around its car, but reduce the attenuation
            // caused by the top-down camera being high above the street.
            source.spatialBlend = spatialBlend;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = minimumDistance;
            source.maxDistance = maximumDistance;
            source.dopplerLevel = 0.2f;
            source.outputAudioMixerGroup = outputMixerGroup;
            return source;
        }

        public void UpdateEngineSource(AudioSource source, float speed, bool canPlay)
        {
            if (source == null)
            {
                return;
            }

            if (!canPlay || engineLoop == null)
            {
                if (source.isPlaying)
                {
                    source.Stop();
                }

                return;
            }

            float speedFactor = Mathf.Clamp01(speed / speedForMaxPitch);
            source.volume = Mathf.Lerp(idleVolume, maxVolume, speedFactor);
            source.pitch = Mathf.Lerp(idlePitch, maxPitch, speedFactor);
            if (!source.isPlaying)
            {
                source.Play();
            }
        }
    }
}
