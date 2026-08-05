using UnityEngine;

namespace FriendOfOurs.Scenes
{
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
        [SerializeField] private GameObject pauseScreen;
        [SerializeField] private GameObject pauseMainPanel;
        [SerializeField] private GameObject pauseSettingsPanel;
        [SerializeField] private AudioSettingsController audioSettings;
        [SerializeField] private FullSceneLoadingTransition loadingTransition;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private bool isPaused;

        private void Awake()
        {
            if (loadingTransition == null)
            {
                loadingTransition = FindFirstObjectByType<FullSceneLoadingTransition>();
            }

            if (audioSettings == null)
            {
                audioSettings = FindFirstObjectByType<AudioSettingsController>();
            }

            SetPaused(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(pauseKey))
            {
                SetPaused(!isPaused);
            }
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        public void ResumeGame()
        {
            SetPaused(false);
        }

        public void ShowSettings()
        {
            if (!isPaused)
            {
                return;
            }

            SetPanelActive(pauseMainPanel, false);
            SetPanelActive(pauseSettingsPanel, true);
            audioSettings?.RefreshSliders();
        }

        public void ShowPauseMainPanel()
        {
            if (!isPaused)
            {
                return;
            }

            SetPanelActive(pauseMainPanel, true);
            SetPanelActive(pauseSettingsPanel, false);
        }

        public void ReturnToMainMenu()
        {
            if (loadingTransition == null)
            {
                Debug.LogError("Pause menu cannot return to the main menu because no FullSceneLoadingTransition exists.", this);
                return;
            }

            // Keep gameplay and its audio paused while the loading UI is shown.
            // The current scene is destroyed on activation, where OnDisable
            // restores normal time and audio for the main menu.
            if (pauseScreen != null)
            {
                pauseScreen.SetActive(false);
            }
            loadingTransition.LoadScene(mainMenuSceneName);
        }

        private void SetPaused(bool shouldPause)
        {
            isPaused = shouldPause;
            Time.timeScale = shouldPause ? 0f : 1f;
            AudioListener.pause = shouldPause;
            if (pauseScreen != null)
            {
                pauseScreen.SetActive(shouldPause);
            }

            if (shouldPause)
            {
                ShowPauseMainPanel();
            }
        }

        private static void SetPanelActive(GameObject panel, bool isActive)
        {
            if (panel != null)
            {
                panel.SetActive(isActive);
            }
        }
    }
}
