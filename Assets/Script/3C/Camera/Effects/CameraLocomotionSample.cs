using UnityEngine;

namespace CGame
{
    public readonly struct CameraLocomotionSample
    {
        public CameraLocomotionSample(float horizontalSpeed, float verticalSpeed, bool isGrounded)
        {
            HorizontalSpeed = Mathf.Max(0f, horizontalSpeed);
            VerticalSpeed = verticalSpeed;
            IsGrounded = isGrounded;
        }

        public float HorizontalSpeed { get; }
        public float VerticalSpeed { get; }
        public bool IsGrounded { get; }

        public static CameraLocomotionSample Idle => new CameraLocomotionSample(0f, 0f, true);
    }
}
