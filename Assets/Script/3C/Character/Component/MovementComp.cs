using UnityEngine;

namespace CGame
{
    public class MovementComp : IComponent, ICharacterPhysicsController
    {
        private Pawn owner;
        private CharacterPhysicsMotor motor;

        public int Priority => 100;
        public float MaxAcceleration { get; set; } = 20f;
        public float GroundFriction { get; set; } = 8f;
        public float BrakingDeceleration { get; set; } = 20f;
        public float Mass { get; set; } = 100f;
        public float JumpSpeed { get; set; } = 7f;
        public float Gravity { get; set; } = 20f;

        public void BindingMotor(CharacterPhysicsMotor characterMotor)
        {
            motor = characterMotor;
        }

        public void InitializingComponent(Pawn pawn)
        {
            owner = pawn;
        }

        public void UpdatingComponent(float elapseSeconds)
        {
        }

        public void FixedUpdatingComponent(float elapseSeconds)
        {
        }

        public void LateUpdatingComponent(float elapseSeconds)
        {
        }

        public void ShuttingDownComponent()
        {
            owner = null;
            motor = null;
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (owner == null || deltaTime <= 0f)
            {
                return;
            }

            Vector3 up = Vector3.up;
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(currentVelocity, up);
            Vector3 verticalVelocity = Vector3.Project(currentVelocity, up);
            Vector3 movementInput = owner.ConsumingMovementInput();
            if (movementInput.sqrMagnitude > 0f)
            {
                horizontalVelocity *= Mathf.Clamp01(1f - GroundFriction * deltaTime);
                horizontalVelocity += Vector3.ProjectOnPlane(movementInput, up) * MaxAcceleration * deltaTime;
            }
            else
            {
                float speedDrop = BrakingDeceleration * deltaTime;
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, speedDrop);
                horizontalVelocity *= Mathf.Clamp01(1f - GroundFriction * deltaTime);
            }

            bool isGrounded = motor == null || motor.GroundingStatus.IsStableOnGround;
            bool jumpRequested = owner.ConsumingJumpInput();
            if (jumpRequested && isGrounded && motor != null)
            {
                verticalVelocity = up * JumpSpeed;
                motor.ForceUnground();
                isGrounded = false;
            }
            else if (isGrounded)
            {
                verticalVelocity = Vector3.zero;
            }

            if (!isGrounded)
            {
                verticalVelocity -= up * Gravity * deltaTime;
            }

            currentVelocity = horizontalVelocity + verticalVelocity;
            currentVelocity += owner.ConsumingForce() / Mathf.Max(Mass, Mathf.Epsilon) * deltaTime;
            currentVelocity += owner.ConsumingImpulse() / Mathf.Max(Mass, Mathf.Epsilon);
        }

        public void BeforeCharacterUpdate(float deltaTime) { }
        public void PostGroundingUpdate(float deltaTime) { }
        public void AfterCharacterUpdate(float deltaTime) { }
        public bool IsColliderValidForCollisions(Collider coll) => coll != null;
        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
        public void OnDiscreteCollisionDetected(Collider hitCollider) { }
    }
}
