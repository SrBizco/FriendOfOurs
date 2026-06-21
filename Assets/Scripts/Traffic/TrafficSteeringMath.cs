using UnityEngine;

namespace FriendOfOurs.Traffic
{
    public static class TrafficSteeringMath
    {
        public static float GetSignedSteerAngle(Vector3 forward, Vector3 directionToTarget, float maxSteerAngle)
        {
            forward.y = 0f;
            directionToTarget.y = 0f;

            if (forward.sqrMagnitude <= 0.0001f || directionToTarget.sqrMagnitude <= 0.0001f)
            {
                return 0f;
            }

            float signedAngle = Vector3.SignedAngle(forward.normalized, directionToTarget.normalized, Vector3.up);
            return Mathf.Clamp(signedAngle, -maxSteerAngle, maxSteerAngle);
        }

        public static float GetAccelerationInput(float currentSpeed, float targetSpeed)
        {
            if (targetSpeed <= 0f || currentSpeed >= targetSpeed)
            {
                return 0f;
            }

            return 1f;
        }
    }
}
