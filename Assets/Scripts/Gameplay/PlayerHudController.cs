using Michsky.MUIP;
using TMPro;
using UnityEngine;

namespace FriendOfOurs.Gameplay
{
    public sealed class PlayerHudController : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private Health playerHealth;
        [SerializeField] private PlayerWallet playerWallet;

        [Header("HUD")]
        [SerializeField] private ProgressBar healthBar;
        [SerializeField] private TMP_Text moneyText;
        [SerializeField] private string moneyPrefix = "$ ";

        private void Awake()
        {
            if (playerHealth == null)
            {
                PlayerController player = FindFirstObjectByType<PlayerController>();
                playerHealth = player != null ? player.GetComponent<Health>() : null;
            }

            if (playerWallet == null)
            {
                playerWallet = FindFirstObjectByType<PlayerWallet>();
            }
        }

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.Changed += UpdateHealth;
            }

            if (playerWallet != null)
            {
                playerWallet.MoneyChanged += UpdateMoney;
            }

            RefreshHud();
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.Changed -= UpdateHealth;
            }

            if (playerWallet != null)
            {
                playerWallet.MoneyChanged -= UpdateMoney;
            }
        }

        public void RefreshHud()
        {
            if (playerHealth != null)
            {
                UpdateHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }

            if (playerWallet != null)
            {
                UpdateMoney(playerWallet.Money);
            }
        }

        private void UpdateHealth(float currentHealth, float maxHealth)
        {
            if (healthBar == null)
            {
                return;
            }

            float normalizedHealth = maxHealth <= 0f ? 0f : Mathf.Clamp01(currentHealth / maxHealth);
            healthBar.isOn = false;
            healthBar.ChangeValue(Mathf.Lerp(healthBar.minValue, healthBar.maxValue, normalizedHealth));
        }

        private void UpdateMoney(int money)
        {
            if (moneyText != null)
            {
                moneyText.text = $"{moneyPrefix}{money}";
            }
        }
    }
}
