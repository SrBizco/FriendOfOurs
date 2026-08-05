using System;
using FriendOfOurs.NPCs;
using UnityEngine;

namespace FriendOfOurs.Gameplay
{
    public sealed class WantedSystem : MonoBehaviour
    {
        public static WantedSystem Active { get; private set; }

        [SerializeField] private Transform player;
        [SerializeField] private PoliceSpawner policeSpawner;
        [SerializeField, Range(1, 5)] private int maximumLevel = 5;
        [SerializeField, Min(1f)] private float escapePoliceDistance = 24f;
        [SerializeField, Min(0.1f)] private float escapeDuration = 12f;

        private int wantedLevel;
        private float escapeTime;

        public event Action<int> LevelChanged;
        public int WantedLevel => wantedLevel;
        public bool IsWanted => wantedLevel > 0;

        private void Awake()
        {
            Active = this;
            if (player == null)
            {
                PlayerController playerController = FindFirstObjectByType<PlayerController>();
                player = playerController != null ? playerController.transform : null;
            }

            if (policeSpawner == null)
            {
                policeSpawner = FindFirstObjectByType<PoliceSpawner>();
            }
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        private void Update()
        {
            if (!IsWanted || player == null)
            {
                return;
            }

            if (policeSpawner == null)
            {
                policeSpawner = FindFirstObjectByType<PoliceSpawner>();
            }

            if (policeSpawner != null && policeSpawner.HasActiveOfficerNear(player.position, escapePoliceDistance))
            {
                escapeTime = 0f;
                return;
            }

            escapeTime += Time.deltaTime;
            if (escapeTime >= escapeDuration)
            {
                SetWantedLevel(0);
            }
        }

        public void ReportMurder(Transform attacker)
        {
            if (attacker == null || player == null || (attacker != player && !attacker.IsChildOf(player)))
            {
                return;
            }

            escapeTime = 0f;
            SetWantedLevel(Mathf.Min(wantedLevel + 1, maximumLevel));
        }

        private void SetWantedLevel(int value)
        {
            value = Mathf.Clamp(value, 0, maximumLevel);
            if (wantedLevel == value)
            {
                return;
            }

            wantedLevel = value;
            escapeTime = 0f;
            LevelChanged?.Invoke(wantedLevel);
        }
    }
}
