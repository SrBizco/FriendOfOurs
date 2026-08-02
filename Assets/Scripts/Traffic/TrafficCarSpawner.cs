using System.Collections.Generic;
using UnityEngine;

namespace FriendOfOurs.Traffic
{
    /// <summary>Spawns and recycles physical NPC cars only outside the player's camera view.</summary>
    public sealed class TrafficCarSpawner : MonoBehaviour
    {
        [SerializeField] private TrafficNetwork trafficNetwork;
        [SerializeField] private Transform player;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private NpcCarController[] carPrefabs;
        [SerializeField, Min(0)] private int maxAlive = 8;
        [SerializeField, Min(0.1f)] private float spawnInterval = 1f;
        [SerializeField, Min(1f)] private float minSpawnDistance = 28f;
        [SerializeField, Min(2f)] private float maxSpawnDistance = 70f;
        [SerializeField, Min(2f)] private float despawnDistance = 90f;
        [SerializeField, Range(1, 64)] private int spawnAttemptsPerTick = 12;
        [SerializeField, Range(0f, 0.5f)] private float cameraMargin = 0.08f;
        [Header("Spawn clearance")]
        [SerializeField] private LayerMask occupancyLayers = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0.1f)] private float spawnClearanceRadius = 4.5f;

        private readonly List<NpcCarController> cars = new List<NpcCarController>();
        private readonly Collider[] overlapHits = new Collider[24];
        private float nextSpawnTime;

        private void Awake()
        {
            if (trafficNetwork == null)
            {
                trafficNetwork = TrafficNetwork.Active;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void Update()
        {
            RecycleDistantCars();
            if (Time.time < nextSpawnTime || CountActiveCars() >= maxAlive)
            {
                return;
            }

            nextSpawnTime = Time.time + spawnInterval;
            TrySpawnCar();
        }

        private void TrySpawnCar()
        {
            if (trafficNetwork == null || player == null || carPrefabs == null || carPrefabs.Length == 0)
            {
                return;
            }

            if (!trafficNetwork.TryGetSpawnPose(player.position, targetCamera, minSpawnDistance, maxSpawnDistance,
                    cameraMargin, spawnAttemptsPerTick, out Pose pose, out int edgeIndex))
            {
                return;
            }

            if (!IsSpawnClear(pose.position))
            {
                return;
            }

            NpcCarController car = GetInactiveCar();
            if (car == null)
            {
                NpcCarController prefab = carPrefabs[Random.Range(0, carPrefabs.Length)];
                if (prefab == null)
                {
                    return;
                }

                car = Instantiate(prefab, pose.position, pose.rotation, transform);
                car.gameObject.SetActive(false);
                cars.Add(car);
            }

            car.transform.SetPositionAndRotation(pose.position, pose.rotation);
            car.SetTrafficSpawnerOwner(this);
            car.InitializeTrafficRoute(trafficNetwork, edgeIndex);
            car.gameObject.SetActive(true);
        }

        public bool TryRecyclePlayerVehicle(NpcCarController car)
        {
            if (car == null || !cars.Contains(car))
            {
                return false;
            }

            car.gameObject.SetActive(false);
            return true;
        }

        private void RecycleDistantCars()
        {
            if (player == null)
            {
                return;
            }

            float squaredDespawnDistance = despawnDistance * despawnDistance;
            for (int i = 0; i < cars.Count; i++)
            {
                NpcCarController car = cars[i];
                if (car == null || !car.gameObject.activeSelf)
                {
                    continue;
                }

                if ((car.transform.position - player.position).sqrMagnitude <= squaredDespawnDistance || IsVisible(car.transform.position))
                {
                    continue;
                }

                car.gameObject.SetActive(false);
            }
        }

        private NpcCarController GetInactiveCar()
        {
            for (int i = 0; i < cars.Count; i++)
            {
                if (cars[i] != null && !cars[i].gameObject.activeSelf)
                {
                    return cars[i];
                }
            }

            return null;
        }

        private int CountActiveCars()
        {
            int count = 0;
            for (int i = 0; i < cars.Count; i++)
            {
                if (cars[i] != null && cars[i].gameObject.activeSelf)
                {
                    count++;
                }
            }

            return count;
        }

        private bool IsVisible(Vector3 worldPosition)
        {
            if (targetCamera == null)
            {
                return false;
            }

            Vector3 viewport = targetCamera.WorldToViewportPoint(worldPosition);
            return viewport.z > 0f
                && viewport.x >= -cameraMargin && viewport.x <= 1f + cameraMargin
                && viewport.y >= -cameraMargin && viewport.y <= 1f + cameraMargin;
        }

        private bool IsSpawnClear(Vector3 position)
        {
            Vector3 center = position + Vector3.up;
            int hitCount = Physics.OverlapSphereNonAlloc(center, spawnClearanceRadius, overlapHits, occupancyLayers,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = overlapHits[i];
                if (hit != null && hit.GetComponentInParent<NpcCarController>() != null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
