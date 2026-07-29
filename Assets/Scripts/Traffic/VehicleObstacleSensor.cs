using UnityEngine;

namespace FriendOfOurs.Traffic
{
    public sealed class VehicleObstacleSensor : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float detectionDistance = 14f;
        [SerializeField, Min(0.1f)] private float stopDistance = 4.5f;
        [SerializeField, Min(0.1f)] private float slowDistance = 12f;
        [SerializeField, Min(0.05f)] private float castRadius = 1.1f;
        [SerializeField] private Vector3 localOrigin = new Vector3(0f, 0.8f, 2.2f);
        [SerializeField] private LayerMask obstacleLayers = Physics.DefaultRaycastLayers;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
        [SerializeField] private bool detectOnlyNpcCars;
        [SerializeField] private bool drawDebugGizmos = true;

        private readonly RaycastHit[] hits = new RaycastHit[12];
        private TrafficObstacleResponse lastResponse = TrafficObstacleResponse.Clear;

        public TrafficObstacleResponse Scan()
        {
            Vector3 origin = transform.TransformPoint(localOrigin);
            int hitCount = Physics.SphereCastNonAlloc(
                origin,
                castRadius,
                transform.forward,
                hits,
                detectionDistance,
                obstacleLayers,
                triggerInteraction);

            bool hasObstacle = false;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = hits[i].collider;
                if (!CanUseHit(hitCollider, transform, detectOnlyNpcCars))
                {
                    continue;
                }

                if (hits[i].distance < closestDistance)
                {
                    closestDistance = hits[i].distance;
                    hasObstacle = true;
                }
            }

            lastResponse = TrafficObstacleResponse.Calculate(
                hasObstacle,
                closestDistance,
                stopDistance,
                Mathf.Min(slowDistance, detectionDistance));

            return lastResponse;
        }

        public static bool CanUseHit(Collider hitCollider, Transform sensorTransform, bool detectOnlyNpcCars)
        {
            if (hitCollider == null
                || sensorTransform == null
                || hitCollider.transform == sensorTransform
                || hitCollider.transform.IsChildOf(sensorTransform))
            {
                return false;
            }

            if (!detectOnlyNpcCars)
            {
                return true;
            }

            NpcCarController otherCar = hitCollider.GetComponentInParent<NpcCarController>();
            return otherCar != null && otherCar.transform != sensorTransform;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos)
            {
                return;
            }

            Vector3 origin = transform.TransformPoint(localOrigin);
            Vector3 end = origin + transform.forward * detectionDistance;
            Gizmos.color = lastResponse.HasObstacle ? Color.red : Color.blue;
            Gizmos.DrawLine(origin, end);
            Gizmos.DrawWireSphere(origin, castRadius);
            Gizmos.DrawWireSphere(end, castRadius);
        }
    }
}
