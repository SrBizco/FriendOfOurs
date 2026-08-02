using UnityEngine;

namespace FriendOfOurs.Gameplay
{
    public sealed class TopDownCameraController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Orbit")]
        [SerializeField] private float yawDegrees = 45f;
        [SerializeField, Min(1f)] private float distance = 12f;
        [SerializeField, Min(1f)] private float height = 10f;
        [SerializeField, Min(0f)] private float mouseRotationSpeed = 4f;

        [Header("Follow")]
        [SerializeField, Min(0f)] private float positionSmoothTime = 0.03f;
        [SerializeField, Min(0f)] private float rotationSmoothSpeed = 35f;

        [Header("Vehicle follow")]
        [SerializeField, Min(0f)] private float vehicleYawFollowSpeed = 8f;

        private Vector3 positionVelocity;
        private Transform vehicleTarget;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        public void SetVehicleFollow(Transform vehicle)
        {
            vehicleTarget = vehicle;
            target = vehicle;
        }

        public void ClearVehicleFollow(Transform onFootTarget)
        {
            vehicleTarget = null;
            target = onFootTarget;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            if (vehicleTarget != null)
            {
                yawDegrees = Mathf.LerpAngle(
                    yawDegrees,
                    vehicleTarget.eulerAngles.y,
                    vehicleYawFollowSpeed * Time.deltaTime);
            }
            else if (Input.GetMouseButton(1))
            {
                yawDegrees += Input.GetAxis("Mouse X") * mouseRotationSpeed;
            }

            Vector3 desiredPosition = TopDownControlMath.GetCameraPosition(
                target.position,
                yawDegrees,
                distance,
                height);

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref positionVelocity,
                positionSmoothTime);

            Quaternion desiredRotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                desiredRotation,
                rotationSmoothSpeed * Time.deltaTime);
        }
    }
}
