using UnityEngine;

namespace CGame.Animation
{
    public readonly struct CharacterAnimationFrameData
    {
        public CharacterAnimationFrameData(
            Vector3 worldLocation,
            Quaternion worldRotation,
            Vector3 worldVelocity,
            Vector3 localVelocity,
            Vector3 worldAcceleration,
            float displacementSpeed,
            float yawDeltaSpeed,
            bool isGrounded,
            bool isJumping,
            bool isFalling,
            float timeToJumpApex)
        {
            WorldLocation = worldLocation;
            WorldRotation = worldRotation;
            WorldVelocity = worldVelocity;
            LocalVelocity = localVelocity;
            WorldAcceleration = worldAcceleration;
            DisplacementSpeed = displacementSpeed;
            YawDeltaSpeed = yawDeltaSpeed;
            IsGrounded = isGrounded;
            IsJumping = isJumping;
            IsFalling = isFalling;
            TimeToJumpApex = timeToJumpApex;
        }

        public Vector3 WorldLocation { get; }
        public Quaternion WorldRotation { get; }
        public Vector3 WorldVelocity { get; }
        public Vector3 LocalVelocity { get; }
        public Vector3 WorldAcceleration { get; }
        public float DisplacementSpeed { get; }
        public float YawDeltaSpeed { get; }
        public bool IsGrounded { get; }
        public bool IsJumping { get; }
        public bool IsFalling { get; }
        public float TimeToJumpApex { get; }
    }
}
