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

        public void UpdateObserverPresentation(ObserverAimPresentationSnapshot snapshot)
        {
            AnimationGraphContext context = graph.Context;
            context.ObserverBodyYaw = snapshot.BodyYaw;
            context.ObserverAimYawOffset = snapshot.AimYawOffset;
            context.ObserverAimPitch = snapshot.AimPitch;
            context.ObserverAimWeight = snapshot.AimWeight;
            context.ObserverAdsWeight = snapshot.AdsWeight;
            context.ObserverWeaponState = snapshot.WeaponState;
            context.LeftHandIkWeight = snapshot.LeftHandIkWeight;
        }

        public void ClearObserverPresentation()
        {
            AnimationGraphContext context = graph.Context;
            context.ObserverBodyYaw = 0f;
            context.ObserverAimYawOffset = 0f;
            context.ObserverAimPitch = 0f;
            context.ObserverAimWeight = 0f;
            context.ObserverAdsWeight = 0f;
            context.ObserverWeaponState = ObserverWeaponState.Holstered;
            context.LeftHandIkWeight = 0f;
        }

        public void Dispose() => graph.Dispose();
    }
}
