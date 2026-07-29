using UnityEngine;

namespace FriendOfOurs.Traffic
{
    public readonly struct TrafficObstacleResponse
    {
        public static readonly TrafficObstacleResponse Clear = new TrafficObstacleResponse(false, 0f, 1f, false);

        private TrafficObstacleResponse(bool hasObstacle, float obstacleDistance, float speedFactor, bool shouldStop)
        {
            HasObstacle = hasObstacle;
            ObstacleDistance = obstacleDistance;
            SpeedFactor = speedFactor;
            ShouldStop = shouldStop;
        }

        public bool HasObstacle { get; }
        public float ObstacleDistance { get; }
        public float SpeedFactor { get; }
        public bool ShouldStop { get; }

        public static TrafficObstacleResponse Calculate(
            bool hasObstacle,
            float obstacleDistance,
            float stopDistance,
            float slowDistance)
        {
            if (!hasObstacle)
            {
                return Clear;
            }

            stopDistance = Mathf.Max(0f, stopDistance);
            slowDistance = Mathf.Max(stopDistance + 0.01f, slowDistance);
            obstacleDistance = Mathf.Max(0f, obstacleDistance);

            if (obstacleDistance <= stopDistance)
            {
                return new TrafficObstacleResponse(true, obstacleDistance, 0f, true);
            }

            float speedFactor = Mathf.InverseLerp(stopDistance, slowDistance, obstacleDistance);
            return new TrafficObstacleResponse(true, obstacleDistance, speedFactor, false);
        }
    }
}
