using System;
using UnityEngine;

namespace CGame.Animation
{
    public sealed class CharacterAnimInstance : IDisposable
    {
        private readonly CharacterAnimationGraph graph;

        public CharacterAnimInstance(Animator animator, CharacterAnimationConfig config)
        {
            graph = new CharacterAnimationGraph(animator, config);
        }

        public CharacterAnimationFrameData FrameData { get; private set; }
        public CharacterAnimationGraph Graph => graph;

        public void UpdatePhysicalProperties(CharacterAnimationFrameData frameData)
        {
            FrameData = frameData;
            AnimationGraphContext context = graph.Context;
            context.WorldVelocity = frameData.WorldVelocity;
            context.LocalVelocity = frameData.LocalVelocity;
            context.WorldAcceleration = frameData.WorldAcceleration;
            context.MoveSpeed = new Vector2(frameData.LocalVelocity.x, frameData.LocalVelocity.z).magnitude;
            context.VerticalVelocity = frameData.WorldVelocity.y;
            context.IsGrounded = frameData.IsGrounded;
            context.IsJumping = frameData.IsJumping;
            context.IsFalling = frameData.IsFalling;
            context.DisplacementSpeed = frameData.DisplacementSpeed;
            context.YawDeltaSpeed = frameData.YawDeltaSpeed;
            context.TimeToJumpApex = frameData.TimeToJumpApex;
            context.OverlayWeight = 0f;
        }

        public void UpdateAnimation(float deltaTime)
        {
            graph.Update(deltaTime);
        }

        public void ApplyWeaponEquipment(WeaponEquipmentSnapshot snapshot)
        {
            graph.ApplyWeaponEquipment(snapshot);
        }

        public void Dispose() => graph.Dispose();
    }
}
