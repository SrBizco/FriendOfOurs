using TMPro;
using UnityEngine;

namespace FriendOfOurs.Gameplay
{
    public sealed class WantedHudController : MonoBehaviour
    {
        [SerializeField] private WantedSystem wantedSystem;
        [SerializeField] private GameObject wantedPanel;
        [SerializeField] private TMP_Text wantedText;

        private void Awake()
        {
            if (wantedSystem == null) wantedSystem = FindFirstObjectByType<WantedSystem>();
        }

        private void OnEnable()
        {
            if (wantedSystem != null) wantedSystem.LevelChanged += UpdateWantedText;
            Refresh();
        }

        private void OnDisable()
        {
            if (wantedSystem != null) wantedSystem.LevelChanged -= UpdateWantedText;
        }

        public void Refresh()
        {
            UpdateWantedText(wantedSystem != null ? wantedSystem.WantedLevel : 0);
        }

        private void UpdateWantedText(int level)
        {
            if (wantedPanel != null) wantedPanel.SetActive(level > 0);
            if (wantedText != null) wantedText.text = $"WANTED: {level}/5";
        }
    }
}
