using System.Collections;
using UnityEngine;

namespace FriendOfOurs.Scenes
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("Menu Screens")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject creditsPanel;

        [Header("Game Start")]
        [SerializeField] private string gameplaySceneName = "Demo";
        [SerializeField] private FullSceneLoadingTransition loadingTransition;

        private Coroutine panelSwitchRoutine;

        private void Awake()
        {
            if (loadingTransition == null)
            {
                loadingTransition = FindFirstObjectByType<FullSceneLoadingTransition>();
            }

            SetActivePanel(mainPanel);
        }

        public void ShowMainPanel()
        {
            QueuePanelSwitch(mainPanel);
        }

        public void ShowSettingsPanel()
        {
            QueuePanelSwitch(settingsPanel);
        }

        public void ShowCreditsPanel()
        {
            QueuePanelSwitch(creditsPanel);
        }

        public void PlayGame()
        {
            if (loadingTransition == null)
            {
                Debug.LogError("Main menu cannot start the game because no FullSceneLoadingTransition exists.", this);
                return;
            }

            SetPanelActive(mainPanel, false);
            SetPanelActive(settingsPanel, false);
            SetPanelActive(creditsPanel, false);
            loadingTransition.LoadScene(gameplaySceneName);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            Debug.Log("Quit requested. Application.Quit only closes a built player.");
#else
            Application.Quit();
#endif
        }

        private void SetActivePanel(GameObject panelToShow)
        {
            if (panelToShow == null)
            {
                Debug.LogError("Main menu cannot show a panel because its reference is missing.", this);
                return;
            }

            SetPanelActive(mainPanel, panelToShow == mainPanel);
            SetPanelActive(settingsPanel, panelToShow == settingsPanel);
            SetPanelActive(creditsPanel, panelToShow == creditsPanel);
        }

        private void QueuePanelSwitch(GameObject panelToShow)
        {
            if (panelSwitchRoutine != null)
            {
                StopCoroutine(panelSwitchRoutine);
            }

            panelSwitchRoutine = StartCoroutine(SwitchPanelAfterClick(panelToShow));
        }

        private IEnumerator SwitchPanelAfterClick(GameObject panelToShow)
        {
            yield return null;
            SetActivePanel(panelToShow);
            panelSwitchRoutine = null;
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
