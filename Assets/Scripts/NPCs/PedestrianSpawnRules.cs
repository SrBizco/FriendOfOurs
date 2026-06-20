using UnityEngine;

namespace FriendOfOurs.NPCs
{
    public static class PedestrianSpawnRules
    {
        public static bool IsVisibleInViewport(Vector3 viewportPoint, float margin)
        {
            return viewportPoint.z > 0f
                && viewportPoint.x >= -margin
                && viewportPoint.x <= 1f + margin
                && viewportPoint.y >= -margin
                && viewportPoint.y <= 1f + margin;
        }

        public static bool IsWithinSpawnRing(Vector3 origin, Vector3 candidate, float minDistance, float maxDistance)
        {
            Vector2 origin2D = new Vector2(origin.x, origin.z);
            Vector2 candidate2D = new Vector2(candidate.x, candidate.z);
            float distance = Vector2.Distance(origin2D, candidate2D);

            return distance >= minDistance && distance <= maxDistance;
        }

        public static bool ShouldDespawn(float distanceFromPlayer, bool isVisible, float despawnDistance)
        {
            return distanceFromPlayer >= despawnDistance && !isVisible;
        }
    }
}
