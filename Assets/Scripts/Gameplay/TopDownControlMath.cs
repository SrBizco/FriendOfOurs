using UnityEngine;

namespace FriendOfOurs.Gameplay
{
    public static class TopDownControlMath
    {
        public static Vector3 GetWorldMoveDirection(Vector2 input)
        {
            Vector3 direction = new Vector3(input.x, 0f, input.y);
            return direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }

        public static Vector3 GetCameraRelativeMoveDirection(Vector2 input, Vector3 cameraForward)
        {
            Vector3 forward = Vector3.ProjectOnPlane(cameraForward, Vector3.up).normalized;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }

            Vector3 right = new Vector3(forward.z, 0f, -forward.x);
            Vector3 direction = right * input.x + forward * input.y;

            return direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }

        public static float GetAnimationSpeed(float inputMagnitude, bool isSprinting)
        {
            if (inputMagnitude <= 0.0001f)
            {
                return 0f;
            }

            return isSprinting ? 1f : 0.5f;
        }

        public static Vector3 GetCameraPosition(Vector3 targetPosition, float yawDegrees, float distance, float height)
        {
            Quaternion yawRotation = Quaternion.Euler(0f, yawDegrees, 0f);
            Vector3 horizontalOffset = yawRotation * new Vector3(0f, 0f, -distance);
            return targetPosition + horizontalOffset + Vector3.up * height;
        }
    }
}
