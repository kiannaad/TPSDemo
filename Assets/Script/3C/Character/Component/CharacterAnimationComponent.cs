using CGame.Animation;
using UnityEngine;

namespace CGame
{
    public sealed class CharacterAnimationComponent : IComponent
    {
        private readonly Animator animator;
        private readonly CharacterPhysicsMotor motor;
        private readonly MovementComp movementComp;
        private readonly CharacterAnimationConfig config;
        private Pawn owner;
        private CharacterAnimInstance animInstance;
        private Vector3 previousLocation;
        private Quaternion previousRotation;
        private Vector3 previousVelocity;
        private bool isFirstUpdate = true;

        public CharacterAnimationComponent(
            Animator animator,
            CharacterPhysicsMotor motor,
            MovementComp movementComp,
            CharacterAnimationConfig config)
        {
            this.animator = animator;
            this.motor = motor;
            this.movementComp = movementComp;
            this.config = config;
        }

        public int Priority => 10;
        public CharacterAnimInstance AnimInstance => animInstance;

        public void InitializingComponent(Pawn pawn)
        {
            owner = pawn;
            animInstance = new CharacterAnimInstance(animator, config);
            previousLocation = motor.transform.position;
            previousRotation = motor.transform.rotation;
            previousVelocity = motor.Velocity;
            isFirstUpdate = true;
        }

        public void UpdatingComponent(float elapseSeconds)
        {
        }

        public void FixedUpdatingComponent(float elapseSeconds)
        {
        }

        public void LateUpdatingComponent(float elapseSeconds)
        {
            if (owner == null || animInstance == null || elapseSeconds <= 0f)
            {
                return;
            }

            Transform characterTransform = motor.transform;
            Vector3 location = characterTransform.position;
            Quaternion rotation = characterTransform.rotation;
            Vector3 velocity = motor.Velocity;
            Vector3 acceleration = isFirstUpdate ? Vector3.zero : (velocity - previousVelocity) / elapseSeconds;
            float displacementSpeed = isFirstUpdate ? 0f : Vector3.ProjectOnPlane(location - previousLocation, Vector3.up).magnitude / elapseSeconds;
            float yawDeltaSpeed = isFirstUpdate ? 0f : Mathf.DeltaAngle(previousRotation.eulerAngles.y, rotation.eulerAngles.y) / elapseSeconds;
            bool isGrounded = motor.GroundingStatus.IsStableOnGround;
            bool isJumping = !isGrounded && velocity.y > 0.01f;
            bool isFalling = !isGrounded && velocity.y <= 0.01f;
            float timeToJumpApex = isJumping ? velocity.y / Mathf.Max(0.001f, movementComp.Gravity) : 0f;
            Vector3 localVelocity = Quaternion.Inverse(rotation) * velocity;
            var frameData = new CharacterAnimationFrameData(
                location,
                rotation,
                velocity,
                localVelocity,
                acceleration,
                displacementSpeed,
                yawDeltaSpeed,
                isGrounded,
                isJumping,
                isFalling,
                timeToJumpApex);
            animInstance.UpdatePhysicalProperties(frameData);
            animInstance.UpdateAnimation(elapseSeconds);
            previousLocation = location;
            previousRotation = rotation;
            previousVelocity = velocity;
            isFirstUpdate = false;
        }

        public void ShuttingDownComponent()
        {
            animInstance?.Dispose();
            animInstance = null;
            owner = null;
        }
    }
}
