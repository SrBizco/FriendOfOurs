using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace FriendOfOurs.Scenes
{
    [DefaultExecutionOrder(-100)]
    public sealed class AudioSettingsController : MonoBehaviour
    {
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string masterParameter = "MasterVolume";
        [SerializeField] private string musicParameter = "MusicVolume";
        [SerializeField] private string sfxParameter = "SfxVolume";
        [SerializeField, Min(0.01f)] private float sliderMaximumValue = 100f;
        [SerializeField, Range(-80f, 0f)] private float minimumDecibels = -80f;
        [Header("Optional UI Sliders")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;

        private static bool hasRuntimeValues;
        private static float masterValue;
        private static float musicValue;
        private static float sfxValue;

        private void Awake()
        {
            if (!hasRuntimeValues)
            {
                masterValue = ReadVolume(masterParameter);
                musicValue = ReadVolume(musicParameter);
                sfxValue = ReadVolume(sfxParameter);
                hasRuntimeValues = true;
            }

            ApplyRuntimeValues();
            RefreshSliders();
        }

        public void SetMasterVolume(float normalizedValue)
        {
            masterValue = ClampSliderValue(normalizedValue);
            SetVolume(masterParameter, masterValue);
        }

        public void SetMusicVolume(float normalizedValue)
        {
            musicValue = ClampSliderValue(normalizedValue);
            SetVolume(musicParameter, musicValue);
        }

        public void SetSfxVolume(float normalizedValue)
        {
            sfxValue = ClampSliderValue(normalizedValue);
            SetVolume(sfxParameter, sfxValue);
        }

        public void RefreshSliders()
        {
            SetSliderValue(masterSlider, masterValue);
            SetSliderValue(musicSlider, musicValue);
            SetSliderValue(sfxSlider, sfxValue);
        }

        private void ApplyRuntimeValues()
        {
            SetVolume(masterParameter, masterValue);
            SetVolume(musicParameter, musicValue);
            SetVolume(sfxParameter, sfxValue);
        }

        private void SetVolume(string parameterName, float sliderValue)
        {
            if (audioMixer == null || string.IsNullOrWhiteSpace(parameterName))
            {
                return;
            }

            float clampedValue = Mathf.Clamp01(sliderValue / sliderMaximumValue);
            float decibels = clampedValue <= 0.0001f
                ? minimumDecibels
                : Mathf.Log10(clampedValue) * 20f;
            audioMixer.SetFloat(parameterName, decibels);
        }

        private float ReadVolume(string parameterName)
        {
            if (audioMixer == null || string.IsNullOrWhiteSpace(parameterName) ||
                !audioMixer.GetFloat(parameterName, out float decibels))
            {
                return sliderMaximumValue;
            }

            if (decibels <= minimumDecibels)
            {
                return 0f;
            }

            return Mathf.Clamp(Mathf.Pow(10f, decibels / 20f) * sliderMaximumValue, 0f, sliderMaximumValue);
        }

        private float ClampSliderValue(float value)
        {
            return Mathf.Clamp(value, 0f, sliderMaximumValue);
        }

        private static void SetSliderValue(Slider slider, float value)
        {
            if (slider != null)
            {
                slider.SetValueWithoutNotify(value);
            }
        }
    }
}
