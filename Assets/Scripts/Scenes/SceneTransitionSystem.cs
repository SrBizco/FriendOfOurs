using System.Collections;
using FriendOfOurs.Gameplay;
using Michsky.MUIP;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FriendOfOurs.Scenes
{
    public sealed class SceneTransitionSystem : MonoBehaviour
    {
        [Header("Fake loading screen")]
        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private ProgressBar loadingBar;
        [SerializeField] private TMP_Text loadingLabel;
        [SerializeField, Min(1f)] private float minimumLoadingDuration = 1f;

        private static SceneTransitionSystem instance;
        private bool isTransitioning;
        private ModalWindowManager loadingScreenModal;
        private Animator loadingScreenAnimator;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DisableResidualModalBehaviour();
            SetLoadingProgress(0f);
            SetLoadingScreenVisible(false);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public static void RequestTransition(AdditiveScenePortal portal, PlayerController player)
        {
            if (instance == null)
            {
                Debug.LogError("Scene transition requested, but no SceneTransitionSystem exists in the main scene.", portal);
                return;
            }

            instance.TryStartTransition(portal, player);
        }

        private void TryStartTransition(AdditiveScenePortal portal, PlayerController player)
        {
            if (isTransitioning || portal == null || player == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(portal.DestinationScene))
            {
                Debug.LogError("The portal has no destination scene assigned.", portal);
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(portal.DestinationScene))
            {
                Debug.LogError(
                    $"Scene '{portal.DestinationScene}' is not available. Add it to Build Settings before testing the portal.",
                    portal);
                return;
            }

            StartCoroutine(TransitionRoutine(portal, player));
        }

        private IEnumerator TransitionRoutine(AdditiveScenePortal portal, PlayerController player)
        {
            isTransitioning = true;
            Scene sourceScene = portal.gameObject.scene;
            PlayerStateSnapshot playerState = new PlayerStateSnapshot(player);
            playerState.Lock();

            SetLoadingScreenVisible(true);
            SetLoadingProgress(0f);

            float elapsed = 0f;
            Scene destinationScene = SceneManager.GetSceneByName(portal.DestinationScene);
            AsyncOperation loadOperation = null;
            if (!destinationScene.isLoaded)
            {
                loadOperation = SceneManager.LoadSceneAsync(portal.DestinationScene, LoadSceneMode.Additive);
            }

            while (elapsed < minimumLoadingDuration || (loadOperation != null && !loadOperation.isDone))
            {
                elapsed += Time.unscaledDeltaTime;
                float fakeProgress = Mathf.Clamp01(elapsed / minimumLoadingDuration);
                if (loadOperation != null && !loadOperation.isDone)
                {
                    fakeProgress = Mathf.Min(fakeProgress, 0.95f);
                }

                SetLoadingProgress(fakeProgress);
                yield return null;
            }

            destinationScene = SceneManager.GetSceneByName(portal.DestinationScene);
            SetLoadingProgress(1f);
            yield return null;

            Transform destinationSpawn = FindSpawnPoint(destinationScene, portal.DestinationSpawnId);
            if (destinationSpawn == null)
            {
                Debug.LogError(
                    $"Spawn point '{portal.DestinationSpawnId}' was not found in scene '{portal.DestinationScene}'.",
                    portal);
            }
            else
            {
                playerState.MoveTo(destinationSpawn.position, destinationSpawn.rotation);

                if (portal.UnloadCurrentSceneAfterArrival && sourceScene.IsValid() && sourceScene.isLoaded)
                {
                    AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(sourceScene);
                    while (unloadOperation != null && !unloadOperation.isDone)
                    {
                        yield return null;
                    }
                }
            }

            SetLoadingScreenVisible(false);
            playerState.Unlock();
            isTransitioning = false;
        }

        private static Transform FindSpawnPoint(Scene scene, string spawnId)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            GameObject[] rootObjects = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < rootObjects.Length; rootIndex++)
            {
                AdditiveSceneSpawnPoint[] spawnPoints = rootObjects[rootIndex].GetComponentsInChildren<AdditiveSceneSpawnPoint>(true);
                for (int spawnIndex = 0; spawnIndex < spawnPoints.Length; spawnIndex++)
                {
                    if (spawnPoints[spawnIndex].SpawnId == spawnId)
                    {
                        return spawnPoints[spawnIndex].transform;
                    }
                }
            }

            return null;
        }

        private void SetLoadingScreenVisible(bool isVisible)
        {
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(isVisible);
            }
        }

        private void DisableResidualModalBehaviour()
        {
            if (loadingScreen == null)
            {
                return;
            }

            loadingScreenModal = loadingScreen.GetComponent<ModalWindowManager>();
            if (loadingScreenModal != null)
            {
                loadingScreenModal.enabled = false;
            }

            loadingScreenAnimator = loadingScreen.GetComponent<Animator>();
            if (loadingScreenAnimator != null)
            {
                loadingScreenAnimator.enabled = false;
            }
        }

        private void SetLoadingProgress(float progress)
        {
            if (loadingBar != null)
            {
                loadingBar.isOn = false;
                loadingBar.ChangeValue(Mathf.Lerp(
                    loadingBar.minValue,
                    loadingBar.maxValue,
                    Mathf.Clamp01(progress)));
            }

            if (loadingLabel != null)
            {
                loadingLabel.text = $"Loading {Mathf.RoundToInt(Mathf.Clamp01(progress) * 100f)}%";
            }
        }

        private sealed class PlayerStateSnapshot
        {
            private readonly PlayerController playerController;
            private readonly PlayerCombatController combatController;
            private readonly PlayerVehicleController vehicleController;
            private readonly GhostController ghostController;
            private readonly Rigidbody body;
            private readonly bool playerControllerWasEnabled;
            private readonly bool combatControllerWasEnabled;
            private readonly bool vehicleControllerWasEnabled;
            private readonly bool ghostControllerWasEnabled;
            private readonly bool bodyWasKinematic;
            private readonly bool bodyUsedGravity;

            public PlayerStateSnapshot(PlayerController player)
            {
                playerController = player;
                combatController = player.GetComponent<PlayerCombatController>();
                vehicleController = player.GetComponent<PlayerVehicleController>();
                ghostController = player.GetComponent<GhostController>();
                body = player.GetComponent<Rigidbody>();
                playerControllerWasEnabled = playerController.enabled;
                combatControllerWasEnabled = combatController != null && combatController.enabled;
                vehicleControllerWasEnabled = vehicleController != null && vehicleController.enabled;
                ghostControllerWasEnabled = ghostController != null && ghostController.enabled;
                bodyWasKinematic = body != null && body.isKinematic;
                bodyUsedGravity = body != null && body.useGravity;
            }

            public void Lock()
            {
                playerController.StopMovement(false);
                playerController.enabled = false;
                if (combatController != null) combatController.enabled = false;
                if (vehicleController != null) vehicleController.enabled = false;
                if (ghostController != null) ghostController.enabled = false;
                if (body != null)
                {
                    body.velocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                    body.isKinematic = true;
                    body.useGravity = false;
                }
            }

            public void MoveTo(Vector3 position, Quaternion rotation)
            {
                if (body != null)
                {
                    body.position = position;
                    body.rotation = rotation;
                    return;
                }

                playerController.transform.SetPositionAndRotation(position, rotation);
            }

            public void Unlock()
            {
                if (body != null)
                {
                    body.velocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                    body.isKinematic = bodyWasKinematic;
                    body.useGravity = bodyUsedGravity;
                }

                playerController.enabled = playerControllerWasEnabled;
                if (combatController != null) combatController.enabled = combatControllerWasEnabled;
                if (vehicleController != null) vehicleController.enabled = vehicleControllerWasEnabled;
                if (ghostController != null) ghostController.enabled = ghostControllerWasEnabled;
            }
        }
    }
}
