using UnityEngine;

namespace FriendOfOurs.NPCs
{
    public static class PedestrianRouteRules
    {
        public static Vector3 NormalizePlanarDirection(Vector3 direction, Vector3 fallback)
        {
            Vector3 planarDirection = new Vector3(direction.x, 0f, direction.z);
            if (planarDirection.sqrMagnitude > 0.0001f)
            {
                return planarDirection.normalized;
            }

            Vector3 planarFallback = new Vector3(fallback.x, 0f, fallback.z);
            if (planarFallback.sqrMagnitude > 0.0001f)
            {
                return planarFallback.normalized;
            }

            return Vector3.forward;
        }

        public static Vector3 BuildCandidatePoint(Vector3 origin, Vector3 direction, float distance)
        {
            Vector3 planarDirection = NormalizePlanarDirection(direction, Vector3.forward);
            return origin + planarDirection * distance;
        }

        public static bool HasMovedEnough(Vector3 previousPosition, Vector3 currentPosition, float minimumDistance)
        {
            Vector2 previous = new Vector2(previousPosition.x, previousPosition.z);
            Vector2 current = new Vector2(currentPosition.x, currentPosition.z);
            return Vector2.Distance(previous, current) >= minimumDistance;
        }
    }
}
