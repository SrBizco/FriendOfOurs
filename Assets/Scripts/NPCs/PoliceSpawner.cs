using System.Collections.Generic;
using FriendOfOurs.Gameplay;
using UnityEngine;
using UnityEngine.AI;

namespace FriendOfOurs.NPCs
{
    public sealed class PoliceSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private WantedSystem wantedSystem;
        [SerializeField] private Transform spawnParent;
        [SerializeField] private List<GameObject> policePrefabs = new List<GameObject>();

        [Header("Officers by wanted level (0-5)")]
        [SerializeField] private int[] officersByWantedLevel = { 0, 1, 2, 3, 4, 5 };
        [SerializeField, Min(0.05f)] private float spawnInterval = 0.6f;
        [SerializeField, Min(1f)] private float minSpawnDistance = 12f;
        [SerializeField, Min(1f)] private float maxSpawnDistance = 30f;
        [SerializeField, Min(1f)] private float despawnDistance = 48f;

        [Header("Spawn validation")]
        [SerializeField, Min(0f)] private float cameraMargin = 0.1f;
        [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 8f;
        [SerializeField, Min(1)] private int spawnAttemptsPerTick = 40;
        [SerializeField] private int navMeshAreaMask = NavMesh.AllAreas;

        private readonly List<PoliceOfficerController> activeOfficers = new List<PoliceOfficerController>();
        private readonly Dictionary<GameObject, Queue<PoliceOfficerController>> pools = new Dictionary<GameObject, Queue<PoliceOfficerController>>();
        private readonly Dictionary<PoliceOfficerController, GameObject> prefabByInstance = new Dictionary<PoliceOfficerController, GameObject>();
        private float nextSpawnTime;

        private void Awake()
        {
            if (player == null)
            {
                PlayerController playerController = FindFirstObjectByType<PlayerController>();
                player = playerController != null ? playerController.transform : null;
            }

            if (targetCamera == null) targetCamera = Camera.main;
            if (wantedSystem == null) wantedSystem = FindFirstObjectByType<WantedSystem>();
            if (spawnParent == null) spawnParent = transform;
        }

        private void Update()
        {
            if (player == null || targetCamera == null || wantedSystem == null)
            {
                return;
            }

            DespawnDeadAndFarOfficers();
            int desiredCount = GetDesiredOfficerCount();
            RetireSurplusOfficers(desiredCount);

            if (activeOfficers.Count >= desiredCount || Time.time < nextSpawnTime || policePrefabs.Count == 0)
            {
                return;
            }

            nextSpawnTime = Time.time + spawnInterval;
            TrySpawnOfficer();
        }

        public bool HasActiveOfficerNear(Vector3 position, float radius)
        {
            float radiusSqr = radius * radius;
            for (int i = activeOfficers.Count - 1; i >= 0; i--)
            {
                PoliceOfficerController officer = activeOfficers[i];
                if (officer == null)
                {
                    activeOfficers.RemoveAt(i);
                    continue;
                }

                if (!officer.Pedestrian.IsDead && (officer.transform.position - position).sqrMagnitude <= radiusSqr)
                {
                    return true;
                }
            }

            return false;
        }

        private int GetDesiredOfficerCount()
        {
            int level = Mathf.Clamp(wantedSystem.WantedLevel, 0, 5);
            return officersByWantedLevel != null && officersByWantedLevel.Length > level
                ? Mathf.Max(0, officersByWantedLevel[level])
                : level;
        }

        private void TrySpawnOfficer()
        {
            for (int i = 0; i < spawnAttemptsPerTick; i++)
            {
                Vector3 candidate = GetRandomCandidateAroundPlayer();
                if (!PedestrianSpawnRules.IsWithinSpawnRing(player.position, candidate, minSpawnDistance, maxSpawnDistance) ||
                    !NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleRadius, navMeshAreaMask) ||
                    IsWorldPointVisible(hit.position))
                {
                    continue;
                }

                SpawnAt(hit.position);
                return;
            }
        }

        private void SpawnAt(Vector3 position)
        {
            GameObject prefab = policePrefabs[Random.Range(0, policePrefabs.Count)];
            PoliceOfficerController officer = GetFromPool(prefab);
            officer.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            officer.gameObject.SetActive(true);
            officer.Pedestrian.Initialize(position, navMeshAreaMask);
            officer.Initialize(player, wantedSystem);
            activeOfficers.Add(officer);
        }

        private PoliceOfficerController GetFromPool(GameObject prefab)
        {
            if (!pools.TryGetValue(prefab, out Queue<PoliceOfficerController> pool))
            {
                pool = new Queue<PoliceOfficerController>();
                pools.Add(prefab, pool);
            }

            while (pool.Count > 0)
            {
                PoliceOfficerController pooled = pool.Dequeue();
                if (pooled != null) return pooled;
            }

            GameObject instance = Instantiate(prefab, spawnParent);
            if (!instance.TryGetComponent(out NavMeshAgent _)) instance.AddComponent<NavMeshAgent>();
            if (!instance.TryGetComponent(out PedestrianController _)) instance.AddComponent<PedestrianController>();
            if (!instance.TryGetComponent(out PoliceOfficerController officer)) officer = instance.AddComponent<PoliceOfficerController>();
            prefabByInstance[officer] = prefab;
            return officer;
        }

        private void RetireSurplusOfficers(int desiredCount)
        {
            for (int i = activeOfficers.Count - 1; i >= 0 && activeOfficers.Count > desiredCount; i--)
            {
                PoliceOfficerController officer = activeOfficers[i];
                if (officer != null && IsWorldPointVisible(officer.transform.position)) continue;
                activeOfficers.RemoveAt(i);
                if (officer != null) ReturnToPool(officer);
            }
        }

        private void DespawnDeadAndFarOfficers()
        {
            for (int i = activeOfficers.Count - 1; i >= 0; i--)
            {
                PoliceOfficerController officer = activeOfficers[i];
                if (officer == null)
                {
                    activeOfficers.RemoveAt(i);
                    continue;
                }

                float distance = Vector3.Distance(player.position, officer.transform.position);
                bool visible = IsWorldPointVisible(officer.transform.position);
                if (!officer.Pedestrian.CanDespawnDead && !PedestrianSpawnRules.ShouldDespawn(distance, visible, despawnDistance))
                {
                    continue;
                }

                activeOfficers.RemoveAt(i);
                ReturnToPool(officer);
            }
        }

        private void ReturnToPool(PoliceOfficerController officer)
        {
            officer.ReleasePursuit();
            officer.gameObject.SetActive(false);
            if (!prefabByInstance.TryGetValue(officer, out GameObject prefab)) return;
            pools[prefab].Enqueue(officer);
        }

        private Vector3 GetRandomCandidateAroundPlayer()
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude <= 0.0001f) direction = Vector2.right;
            float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
            return player.position + new Vector3(direction.x, 0f, direction.y) * distance;
        }

        private bool IsWorldPointVisible(Vector3 worldPoint)
        {
            Vector3 viewportPoint = targetCamera.WorldToViewportPoint(worldPoint);
            return PedestrianSpawnRules.IsVisibleInViewport(viewportPoint, cameraMargin);
        }
    }
}
