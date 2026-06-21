using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace FriendOfOurs.Traffic
{
    [RequireComponent(typeof(SplineContainer))]
    public sealed class TrafficLane : MonoBehaviour
    {
        [SerializeField] private SplineContainer splineContainer;
        [SerializeField, Min(0.1f)] private float gizmoStepDistance = 4f;

        public float Length => splineContainer != null ? splineContainer.CalculateLength() : 0f;

        private void Awake()
        {
            if (splineContainer == null)
            {
                splineContainer = GetComponent<SplineContainer>();
            }
        }

        private void Reset()
        {
            splineContainer = GetComponent<SplineContainer>();
        }

        public Vector3 GetPointAtDistance(float distance)
        {
            if (splineContainer == null)
            {
                return transform.position;
            }

            float t = GetNormalizedDistance(distance);
            return (Vector3)splineContainer.EvaluatePosition(t);
        }

        public Vector3 GetDirectionAtDistance(float distance)
        {
            if (splineContainer == null)
            {
                return transform.forward;
            }

            float t = GetNormalizedDistance(distance);
            Vector3 tangent = (Vector3)splineContainer.EvaluateTangent(t);
            tangent.y = 0f;

            return tangent.sqrMagnitude > 0.0001f ? tangent.normalized : transform.forward;
        }

        public float GetNormalizedDistance(float distance)
        {
            float length = Length;
            if (length <= 0.0001f)
            {
                return 0f;
            }

            return Mathf.Clamp01(distance / length);
        }

        public float FindClosestDistance(Vector3 worldPosition, int sampleCount = 40)
        {
            float length = Length;
            if (splineContainer == null || length <= 0.0001f)
            {
                return 0f;
            }

            sampleCount = Mathf.Max(2, sampleCount);
            float closestDistance = 0f;
            float closestSqrDistance = float.MaxValue;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (sampleCount - 1f);
                Vector3 point = (Vector3)splineContainer.EvaluatePosition(t);
                float sqrDistance = (point - worldPosition).sqrMagnitude;
                if (sqrDistance < closestSqrDistance)
                {
                    closestSqrDistance = sqrDistance;
                    closestDistance = t * length;
                }
            }

            return closestDistance;
        }

        private void OnDrawGizmosSelected()
        {
            if (splineContainer == null)
            {
                splineContainer = GetComponent<SplineContainer>();
            }

            float length = Length;
            if (splineContainer == null || length <= 0.0001f)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Vector3 previous = GetPointAtDistance(0f);
            for (float distance = gizmoStepDistance; distance <= length; distance += gizmoStepDistance)
            {
                Vector3 next = GetPointAtDistance(distance);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }

            Gizmos.DrawLine(previous, GetPointAtDistance(length));

            Gizmos.color = Color.yellow;
            for (float distance = 0f; distance <= length; distance += gizmoStepDistance * 2f)
            {
                Vector3 point = GetPointAtDistance(distance);
                Vector3 direction = GetDirectionAtDistance(distance);
                Gizmos.DrawRay(point + Vector3.up * 0.2f, direction * 1.5f);
            }
        }
    }
}
