using UnityEngine;
using UnityEngine.Audio;

namespace FriendOfOurs.Audio
{
    public sealed class MenuMusicController : MonoBehaviour
    {
        [SerializeField] private AudioClip musicClip;
        [SerializeField] private AudioMixerGroup outputMixerGroup;
        [SerializeField, Range(0f, 1f)] private float volume = 0.7f;

        private AudioSource musicSource;

        private void Awake()
        {
            musicSource = GetComponent<AudioSource>();
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
            }

            musicSource.clip = musicClip;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.volume = volume;
            musicSource.outputAudioMixerGroup = outputMixerGroup;
        }

        private void Start()
        {
            if (musicSource != null && musicSource.clip != null)
            {
                musicSource.Play();
            }
        }

        private void OnDisable()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }
    }
}
