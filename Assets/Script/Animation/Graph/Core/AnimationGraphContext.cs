using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace CGame.Animation
{
    public sealed class AnimationGraphContext
    {
        private const int DebugEventCapacity = 32;
        private readonly List<AnimationDebugEvent> debugEvents = new List<AnimationDebugEvent>(DebugEventCapacity);

        public AnimationGraphContext(Animator animator, PlayableGraph graph)
        {
            Animator = animator;
            Graph = graph;
        }

        public Animator Animator { get; }
        public PlayableGraph Graph { get; }
        public float DeltaTime { get; set; }
        public float MoveSpeed { get; set; }
        public float OverlayWeight { get; set; }
        public float LeftHandIkWeight { get; set; }
        public float AimYaw { get; set; }
        public float AimPitch { get; set; }
        public float AimWeight { get; set; }
        public uint ActiveWeaponGeneration { get; set; }
        public float ObserverBodyYaw { get; set; }
        public float ObserverAimYawOffset { get; set; }
        public float ObserverAimPitch { get; set; }
        public float ObserverAimWeight { get; set; }
        public float ObserverAdsWeight { get; set; }
        public ObserverWeaponState ObserverWeaponState { get; set; }
        public Vector3 WorldVelocity { get; set; }
        public Vector3 LocalVelocity { get; set; }
        public Vector3 WorldAcceleration { get; set; }
        public float VerticalVelocity { get; set; }
        public bool IsGrounded { get; set; }
        public bool IsJumping { get; set; }
        public bool IsFalling { get; set; }
        public float DisplacementSpeed { get; set; }
        public float YawDeltaSpeed { get; set; }
        public float TimeToJumpApex { get; set; }
        public AnimationRootMotionDelta RootMotionDelta { get; set; }
        public float ElapsedTime { get; internal set; }
        public string DebugLocomotionState { get; internal set; } = string.Empty;
        public float DebugFadeProgress { get; internal set; } = 1f;
        public string DebugActiveAction { get; internal set; } = string.Empty;
        public float DebugActiveActionWeight { get; internal set; }
        public IReadOnlyList<AnimationDebugEvent> DebugEvents => debugEvents;
        public long EvaluateFrameId { get; private set; }

        public void BeginEvaluateFrame()
        {
            EvaluateFrameId++;
        }

        public void RecordDebugEvent(string source, string eventName, float value = 0f)
        {
            if (debugEvents.Count == DebugEventCapacity)
            {
                debugEvents.RemoveAt(0);
            }

            debugEvents.Add(new AnimationDebugEvent(ElapsedTime, source, eventName, value));
        }

        public void ResetRootMotionDelta()
        {
            RootMotionDelta = AnimationRootMotionDelta.None;
        }

        public void AccumulateRootMotionDelta(AnimationRootMotionDelta delta)
        {
            if (!delta.IsValid)
            {
                return;
            }

            Vector3 position = RootMotionDelta.PositionDelta + delta.PositionDelta * delta.SourceWeight;
            Quaternion rotation = RootMotionDelta.RotationDelta
                * Quaternion.Slerp(Quaternion.identity, delta.RotationDelta, delta.SourceWeight);
            float weight = Mathf.Clamp01(RootMotionDelta.SourceWeight + delta.SourceWeight);
            RootMotionDelta = new AnimationRootMotionDelta(position, rotation, weight);
        }
    }
}
