using UnityEngine;
using UnityEngine.Audio;

namespace FriendOfOurs.Audio
{
    /// <summary>
    /// Plays player combat feedback from the shared scene audio object.
    /// </summary>
    public sealed class CombatAudioSystem : MonoBehaviour
    {
        public static CombatAudioSystem Active { get; private set; }

        [SerializeField] private AudioClip punchClip;
        [SerializeField] private AudioMixerGroup outputMixerGroup;
        [SerializeField, Range(0f, 1f)] private float volume = 0.65f;

        private AudioSource source;

        private void Awake()
        {
            Active = this;

            // AudioSystem already owns a separate source for footsteps. Combat
            // needs its own channel so a punch never cuts off a footstep.
            source = gameObject.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = outputMixerGroup;
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        public void PlayPunch()
        {
            if (source != null && punchClip != null)
            {
                source.PlayOneShot(punchClip, volume);
            }
        }
    }
}
