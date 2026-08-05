using System.Collections;
using Michsky.MUIP;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FriendOfOurs.Scenes
{
    public sealed class FullSceneLoadingTransition : MonoBehaviour
    {
        [Header("Loading UI")]
        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private ProgressBar loadingBar;
        [SerializeField] private TMP_Text loadingLabel;
        [SerializeField, Min(1f)] private float minimumLoadingDuration = 1f;

        private bool isLoading;

        private void Awake()
        {
            SetProgress(0f);
            SetLoadingScreenVisible(false);
        }

        public void LoadScene(string sceneName)
        {
            if (isLoading || string.IsNullOrWhiteSpace(sceneName))
            {
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"Scene '{sceneName}' is not available. Add it to Build Settings first.", this);
                return;
            }

            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            isLoading = true;
            KeepLoadingUiAlive();

            SetLoadingScreenVisible(true);
            SetProgress(0f);
            yield return null;

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            operation.allowSceneActivation = false;
            float elapsed = 0f;
            while (elapsed < minimumLoadingDuration || operation.progress < 0.9f)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / minimumLoadingDuration);

                if (operation.progress < 0.9f)
                {
                    progress = Mathf.Min(progress, 0.95f);
                }

                SetProgress(progress);
                yield return null;
            }

            SetProgress(1f);
            yield return null;

            operation.allowSceneActivation = true;
            while (!operation.isDone)
            {
                yield return null;
            }

            SetLoadingScreenVisible(false);
            DestroyLoadingUi();
        }

        private void KeepLoadingUiAlive()
        {
            DontDestroyOnLoad(gameObject);

            if (loadingScreen == null)
            {
                return;
            }

            GameObject loadingCanvasRoot = loadingScreen.transform.root.gameObject;
            if (loadingCanvasRoot != gameObject)
            {
                DontDestroyOnLoad(loadingCanvasRoot);
            }
        }

        private void DestroyLoadingUi()
        {
            GameObject loadingCanvasRoot = loadingScreen != null
                ? loadingScreen.transform.root.gameObject
                : null;

            if (loadingCanvasRoot != null && loadingCanvasRoot != gameObject)
            {
                Destroy(loadingCanvasRoot);
            }

            Destroy(gameObject);
        }

        private void SetLoadingScreenVisible(bool isVisible)
        {
            if (loadingScreen == null)
            {
                return;
            }

            loadingScreen.SetActive(isVisible);
        }

        private void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            if (loadingBar != null)
            {
                loadingBar.isOn = false;
                loadingBar.ChangeValue(Mathf.Lerp(loadingBar.minValue, loadingBar.maxValue, progress));
            }

            if (loadingLabel != null)
            {
                loadingLabel.text = $"Loading {Mathf.RoundToInt(progress * 100f)}%";
            }
        }
    }
}
