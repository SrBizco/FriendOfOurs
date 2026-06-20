using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace FriendOfOurs.NPCs
{
    public sealed class PedestrianSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform spawnParent;
        [SerializeField] private List<GameObject> pedestrianPrefabs = new List<GameObject>();

        [Header("Population")]
        [SerializeField, Min(0)] private int maxAlive = 28;
        [SerializeField, Min(0.05f)] private float spawnInterval = 0.2f;
        [SerializeField, Min(1f)] private float minSpawnDistance = 10f;
        [SerializeField, Min(1f)] private float maxSpawnDistance = 32f;
        [SerializeField, Min(1f)] private float despawnDistance = 50f;

        [Header("Spawn Validation")]
        [SerializeField, Min(0f)] private float cameraMargin = 0.1f;
        [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 8f;
        [SerializeField, Min(1)] private int spawnAttemptsPerTick = 40;
        [SerializeField] private int navMeshAreaMask = NavMesh.AllAreas;

        private readonly List<PedestrianController> activePedestrians = new List<PedestrianController>();
        private readonly Dictionary<GameObject, Queue<PedestrianController>> pools = new Dictionary<GameObject, Queue<PedestrianController>>();
        private readonly Dictionary<PedestrianController, GameObject> prefabByInstance = new Dictionary<PedestrianController, GameObject>();
        private float nextSpawnTime;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (spawnParent == null)
            {
                spawnParent = transform;
            }
        }

        private void Update()
        {
            if (player == null || targetCamera == null || pedestrianPrefabs.Count == 0)
            {
                return;
            }

            DespawnFarPedestrians();

            if (activePedestrians.Count >= maxAlive || Time.time < nextSpawnTime)
            {
                return;
            }

            nextSpawnTime = Time.time + spawnInterval;
            TrySpawnPedestrian();
        }

        private void TrySpawnPedestrian()
        {
            for (int i = 0; i < spawnAttemptsPerTick; i++)
            {
                Vector3 candidate = GetRandomCandidateAroundPlayer();
                if (!PedestrianSpawnRules.IsWithinSpawnRing(player.position, candidate, minSpawnDistance, maxSpawnDistance))
                {
                    continue;
                }

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleRadius, navMeshAreaMask))
                {
                    continue;
                }

                if (IsWorldPointVisible(hit.position))
                {
                    continue;
                }

                SpawnAt(hit.position);
                return;
            }
        }

        private Vector3 GetRandomCandidateAroundPlayer()
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector2.right;
            }

            float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
            return player.position + new Vector3(direction.x, 0f, direction.y) * distance;
        }

        private void SpawnAt(Vector3 position)
        {
            GameObject prefab = pedestrianPrefabs[Random.Range(0, pedestrianPrefabs.Count)];
            PedestrianController pedestrian = GetFromPool(prefab);

            pedestrian.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            pedestrian.gameObject.SetActive(true);
            pedestrian.Initialize(position, navMeshAreaMask);
            activePedestrians.Add(pedestrian);
        }

        private PedestrianController GetFromPool(GameObject prefab)
        {
            if (!pools.TryGetValue(prefab, out Queue<PedestrianController> pool))
            {
                pool = new Queue<PedestrianController>();
                pools.Add(prefab, pool);
            }

            while (pool.Count > 0)
            {
                PedestrianController pooled = pool.Dequeue();
                if (pooled != null)
                {
                    return pooled;
                }
            }

            GameObject instance = Instantiate(prefab, spawnParent);
            if (!instance.TryGetComponent(out NavMeshAgent _))
            {
                instance.AddComponent<NavMeshAgent>();
            }

            if (!instance.TryGetComponent(out PedestrianController controller))
            {
                controller = instance.AddComponent<PedestrianController>();
            }

            prefabByInstance[controller] = prefab;
            return controller;
        }

        private void DespawnFarPedestrians()
        {
            for (int i = activePedestrians.Count - 1; i >= 0; i--)
            {
                PedestrianController pedestrian = activePedestrians[i];
                if (pedestrian == null)
                {
                    activePedestrians.RemoveAt(i);
                    continue;
                }

                float distance = Vector3.Distance(player.position, pedestrian.transform.position);
                bool isVisible = IsWorldPointVisible(pedestrian.transform.position);
                if (!PedestrianSpawnRules.ShouldDespawn(distance, isVisible, despawnDistance))
                {
                    continue;
                }

                activePedestrians.RemoveAt(i);
                ReturnToPool(pedestrian);
            }
        }

        private void ReturnToPool(PedestrianController pedestrian)
        {
            pedestrian.gameObject.SetActive(false);

            if (!prefabByInstance.TryGetValue(pedestrian, out GameObject prefab))
            {
                return;
            }

            if (!pools.TryGetValue(prefab, out Queue<PedestrianController> pool))
            {
                pool = new Queue<PedestrianController>();
                pools.Add(prefab, pool);
            }

            pool.Enqueue(pedestrian);
        }

        private bool IsWorldPointVisible(Vector3 worldPoint)
        {
            Vector3 viewportPoint = targetCamera.WorldToViewportPoint(worldPoint);
            return PedestrianSpawnRules.IsVisibleInViewport(viewportPoint, cameraMargin);
        }

        private void OnDrawGizmosSelected()
        {
            if (player == null)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(player.position, minSpawnDistance);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(player.position, maxSpawnDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(player.position, despawnDistance);
        }
    }
}
