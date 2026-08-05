using System.Collections;
using UnityEngine;

namespace FriendOfOurs.Gameplay
{
    public sealed class PlayerDeathController : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private PlayerController player;
        [SerializeField] private Health health;
        [SerializeField] private Transform respawnPoint;

        [Header("Death UI")]
        [SerializeField] private GameObject deathScreen;
        [SerializeField, Min(0f)] private float deathScreenDelay = 2f;

        private Coroutine deathRoutine;
        private bool awaitingRespawn;

        private void Awake()
        {
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
            }

            if (health == null && player != null)
            {
                health = player.GetComponent<Health>();
            }

            SetDeathScreenVisible(false);
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += OnPlayerDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= OnPlayerDied;
            }

            Time.timeScale = 1f;
        }

        public void RespawnPlayer()
        {
            if (!awaitingRespawn)
            {
                return;
            }

            if (player == null || respawnPoint == null || !player.RespawnAt(respawnPoint))
            {
                Debug.LogError("Player respawn requires a PlayerController and a respawn point.", this);
                return;
            }

            awaitingRespawn = false;
            Time.timeScale = 1f;
            SetDeathScreenVisible(false);
        }

        private void OnPlayerDied(DamageInfo damageInfo)
        {
            if (deathRoutine != null || awaitingRespawn)
            {
                return;
            }

            deathRoutine = StartCoroutine(ShowDeathScreenAfterDelay());
        }

        private IEnumerator ShowDeathScreenAfterDelay()
        {
            if (deathScreenDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(deathScreenDelay);
            }

            deathRoutine = null;
            if (health == null || !health.IsDead)
            {
                yield break;
            }

            awaitingRespawn = true;
            SetDeathScreenVisible(true);
            Time.timeScale = 0f;
        }

        private void SetDeathScreenVisible(bool isVisible)
        {
            if (deathScreen != null)
            {
                deathScreen.SetActive(isVisible);
            }
        }
    }
}
