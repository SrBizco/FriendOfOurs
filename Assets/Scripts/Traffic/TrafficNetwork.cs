using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace FriendOfOurs.Traffic
{
    public enum TrafficTurnType
    {
        Straight,
        Left,
        Right
    }

    [Serializable]
    public struct TrafficLaneConnection
    {
        [SerializeField, Min(0)] private int nextLaneIndex;
        [SerializeField] private TrafficTurnType turnType;
        [SerializeField, Min(0f)] private float weight;

        public TrafficLaneConnection(int nextLaneIndex, TrafficTurnType turnType, float weight)
        {
            this.nextLaneIndex = nextLaneIndex;
            this.turnType = turnType;
            this.weight = weight;
        }

        public int NextLaneIndex => nextLaneIndex;
        public TrafficTurnType TurnType => turnType;
        public float Weight => weight;
        public bool IsValid => nextLaneIndex >= 0 && weight > 0f;
    }

    [Serializable]
    public sealed class TrafficLaneData
    {
        [SerializeField] private string name = "Lane";
        [SerializeField, Min(0)] private int splineIndex;
        [SerializeField, Min(0f)] private float speedLimit = 8f;
        [SerializeField] private TrafficLaneConnection[] connections = Array.Empty<TrafficLaneConnection>();

        public TrafficLaneData(string name, int splineIndex, float speedLimit, TrafficLaneConnection[] connections = null)
        {
            this.name = name;
            this.splineIndex = Mathf.Max(0, splineIndex);
            this.speedLimit = Mathf.Max(0f, speedLimit);
            this.connections = connections ?? Array.Empty<TrafficLaneConnection>();
        }

        public string Name => name;
        public int SplineIndex => splineIndex;
        public float SpeedLimit => speedLimit;
        public IReadOnlyList<TrafficLaneConnection> Connections => connections;
    }

    [RequireComponent(typeof(SplineContainer))]
    public sealed class TrafficNetwork : MonoBehaviour
    {
        [SerializeField] private SplineContainer splineContainer;
        [SerializeField] private TrafficLaneData[] lanes = Array.Empty<TrafficLaneData>();
        [SerializeField, Min(0.1f)] private float gizmoStepDistance = 4f;

        public static TrafficNetwork Active { get; private set; }
        public int LaneCount => lanes != null ? lanes.Length : 0;

        private void Awake()
        {
            if (splineContainer == null)
            {
                splineContainer = GetComponent<SplineContainer>();
            }
        }

        private void OnEnable()
        {
            if (splineContainer == null)
            {
                splineContainer = GetComponent<SplineContainer>();
            }

            Active = this;
        }

        private void OnDisable()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        private void Reset()
        {
            splineContainer = GetComponent<SplineContainer>();
        }

        public bool IsValidLaneIndex(int laneIndex)
        {
            return lanes != null
                && laneIndex >= 0
                && laneIndex < lanes.Length
                && splineContainer != null
                && lanes[laneIndex] != null
                && lanes[laneIndex].SplineIndex >= 0
                && lanes[laneIndex].SplineIndex < splineContainer.Splines.Count;
        }

        public float GetLaneLength(int laneIndex)
        {
            if (!IsValidLaneIndex(laneIndex))
            {
                return 0f;
            }

            return splineContainer.CalculateLength(lanes[laneIndex].SplineIndex);
        }

        public float GetLaneSpeedLimit(int laneIndex)
        {
            if (!IsValidLaneIndex(laneIndex))
            {
                return 0f;
            }

            return lanes[laneIndex].SpeedLimit;
        }

        public Vector3 GetPointAtDistance(int laneIndex, float distance)
        {
            if (!IsValidLaneIndex(laneIndex))
            {
                return transform.position;
            }

            float t = GetNormalizedDistance(laneIndex, distance);
            return (Vector3)splineContainer.EvaluatePosition(lanes[laneIndex].SplineIndex, t);
        }

        public Vector3 GetDirectionAtDistance(int laneIndex, float distance)
        {
            if (!IsValidLaneIndex(laneIndex))
            {
                return transform.forward;
            }

            float t = GetNormalizedDistance(laneIndex, distance);
            Vector3 tangent = (Vector3)splineContainer.EvaluateTangent(lanes[laneIndex].SplineIndex, t);
            tangent.y = 0f;

            return tangent.sqrMagnitude > 0.0001f ? tangent.normalized : transform.forward;
        }

        public float GetNormalizedDistance(int laneIndex, float distance)
        {
            float length = GetLaneLength(laneIndex);
            if (length <= 0.0001f)
            {
                return 0f;
            }

            return Mathf.Clamp01(distance / length);
        }

        public float FindClosestDistance(int laneIndex, Vector3 worldPosition, int sampleCount = 40)
        {
            float length = GetLaneLength(laneIndex);
            if (!IsValidLaneIndex(laneIndex) || length <= 0.0001f)
            {
                return 0f;
            }

            sampleCount = Mathf.Max(2, sampleCount);
            float closestDistance = 0f;
            float closestSqrDistance = float.MaxValue;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (sampleCount - 1f);
                Vector3 point = (Vector3)splineContainer.EvaluatePosition(lanes[laneIndex].SplineIndex, t);
                float sqrDistance = (point - worldPosition).sqrMagnitude;
                if (sqrDistance < closestSqrDistance)
                {
                    closestSqrDistance = sqrDistance;
                    closestDistance = t * length;
                }
            }

            return closestDistance;
        }

        public int FindClosestLaneIndex(Vector3 worldPosition, int sampleCount = 40, float maxDistance = float.PositiveInfinity)
        {
            if (lanes == null || lanes.Length == 0)
            {
                return -1;
            }

            sampleCount = Mathf.Max(2, sampleCount);
            float closestSqrDistance = maxDistance >= 0f ? maxDistance * maxDistance : float.PositiveInfinity;
            int closestLaneIndex = -1;

            for (int laneIndex = 0; laneIndex < lanes.Length; laneIndex++)
            {
                if (!IsValidLaneIndex(laneIndex))
                {
                    continue;
                }

                float length = GetLaneLength(laneIndex);
                if (length <= 0.0001f)
                {
                    continue;
                }

                int splineIndex = lanes[laneIndex].SplineIndex;
                for (int i = 0; i < sampleCount; i++)
                {
                    float t = i / (sampleCount - 1f);
                    Vector3 point = (Vector3)splineContainer.EvaluatePosition(splineIndex, t);
                    float sqrDistance = (point - worldPosition).sqrMagnitude;
                    if (sqrDistance < closestSqrDistance)
                    {
                        closestSqrDistance = sqrDistance;
                        closestLaneIndex = laneIndex;
                    }
                }
            }

            return closestLaneIndex;
        }

        public int SelectNextLaneIndex(int laneIndex, float roll)
        {
            if (!IsValidLaneIndex(laneIndex))
            {
                return -1;
            }

            return TrafficLaneSelection.SelectNextLaneIndex(lanes[laneIndex].Connections, LaneCount, roll);
        }

#if UNITY_EDITOR
        public void SetLanesForTests(params TrafficLaneData[] newLanes)
        {
            lanes = newLanes ?? Array.Empty<TrafficLaneData>();
        }
#endif

        private void OnDrawGizmosSelected()
        {
            if (splineContainer == null)
            {
                splineContainer = GetComponent<SplineContainer>();
            }

            if (lanes == null || splineContainer == null)
            {
                return;
            }

            for (int laneIndex = 0; laneIndex < lanes.Length; laneIndex++)
            {
                if (!IsValidLaneIndex(laneIndex))
                {
                    continue;
                }

                DrawLane(laneIndex);
                DrawLaneConnections(laneIndex);
            }
        }

        private void DrawLane(int laneIndex)
        {
            float length = GetLaneLength(laneIndex);
            if (length <= 0.0001f)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Vector3 previous = GetPointAtDistance(laneIndex, 0f);
            for (float distance = gizmoStepDistance; distance <= length; distance += gizmoStepDistance)
            {
                Vector3 next = GetPointAtDistance(laneIndex, distance);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }

            Gizmos.DrawLine(previous, GetPointAtDistance(laneIndex, length));

            Gizmos.color = Color.yellow;
            for (float distance = 0f; distance <= length; distance += gizmoStepDistance * 2f)
            {
                Vector3 point = GetPointAtDistance(laneIndex, distance);
                Vector3 direction = GetDirectionAtDistance(laneIndex, distance);
                Gizmos.DrawRay(point + Vector3.up * 0.2f, direction * 1.5f);
            }
        }

        private void DrawLaneConnections(int laneIndex)
        {
            float length = GetLaneLength(laneIndex);
            Vector3 endPoint = GetPointAtDistance(laneIndex, length);

            Gizmos.color = Color.green;
            IReadOnlyList<TrafficLaneConnection> connections = lanes[laneIndex].Connections;
            for (int i = 0; i < connections.Count; i++)
            {
                int nextLaneIndex = connections[i].NextLaneIndex;
                if (!IsValidLaneIndex(nextLaneIndex))
                {
                    continue;
                }

                Gizmos.DrawLine(
                    endPoint + Vector3.up * 0.4f,
                    GetPointAtDistance(nextLaneIndex, 0f) + Vector3.up * 0.4f);
            }
        }
    }

    public static class TrafficLaneSelection
    {
        public static int SelectNextLaneIndex(
            IReadOnlyList<TrafficLaneConnection> connections,
            int laneCount,
            float roll)
        {
            if (connections == null || connections.Count == 0 || laneCount <= 0)
            {
                return -1;
            }

            float totalWeight = 0f;
            for (int i = 0; i < connections.Count; i++)
            {
                if (IsUsableConnection(connections[i], laneCount))
                {
                    totalWeight += connections[i].Weight;
                }
            }

            if (totalWeight <= 0f)
            {
                return -1;
            }

            float target = Mathf.Clamp01(roll) * totalWeight;
            float accumulated = 0f;

            for (int i = 0; i < connections.Count; i++)
            {
                TrafficLaneConnection connection = connections[i];
                if (!IsUsableConnection(connection, laneCount))
                {
                    continue;
                }

                accumulated += connection.Weight;
                if (target <= accumulated)
                {
                    return connection.NextLaneIndex;
                }
            }

            return -1;
        }

        private static bool IsUsableConnection(TrafficLaneConnection connection, int laneCount)
        {
            return connection.IsValid && connection.NextLaneIndex < laneCount;
        }
    }
}
