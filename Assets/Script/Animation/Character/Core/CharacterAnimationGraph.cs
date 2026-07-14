using System;
using UnityEngine;

namespace CGame.Animation
{
    public sealed class CharacterAnimationGraph : IDisposable
    {
        private readonly OutputNode output;
        private readonly AnimationStateMachineNode locomotionStateMachine;
        private readonly InertializationNode inertializationNode;
        private readonly GameObject leftHandTarget;
        private readonly Transform observerSpine;
        private readonly Transform observerChest;

        public CharacterAnimationGraph(Animator animator, CharacterAnimationConfig config)
        {
            if (animator == null) throw new ArgumentNullException(nameof(animator));
            if (config == null || !config.IsValid) throw new ArgumentException("A valid character animation config is required.", nameof(config));

            var move = new Blend1DNode(new[]
            {
                new Blend1DChild(new ClipNode(config.Walk.AnimationClip), 1f),
                new Blend1DChild(new ClipNode(config.Run.AnimationClip), 3f),
            }, context => context.MoveSpeed);
            AnimationState[] states =
            {
                new AnimationState("Idle", new ClipNode(config.Idle.AnimationClip)),
                new AnimationState("Move", move),
                new AnimationState("Stop", CreateOneShot(config.Stop.AnimationClip)),
                new AnimationState("Air", new ClipNode(config.InAir.AnimationClip)),
                new AnimationState("Land", CreateOneShot(config.Land.AnimationClip)),
            };
            AnimationStateTransition[] transitions =
            {
                new AnimationStateTransition("Idle", "Move", context => context.IsGrounded && context.MoveSpeed > 0.1f),
                new AnimationStateTransition("Move", "Stop", context => context.IsGrounded && context.MoveSpeed <= 0.1f),
                new AnimationStateTransition("Stop", "Move", context => context.IsGrounded && context.MoveSpeed > 0.1f, 10),
                new AnimationStateTransition("Stop", "Idle", context => context.IsGrounded && context.MoveSpeed <= 0.1f),
                new AnimationStateTransition(string.Empty, "Air", context => !context.IsGrounded, 100, 0.08f),
                new AnimationStateTransition("Air", "Land", context => context.IsGrounded, 120, 0.08f),
                new AnimationStateTransition("Land", "Move", context => context.IsGrounded && context.MoveSpeed > 0.1f, 10, 0.1f),
                new AnimationStateTransition("Land", "Idle", context => context.IsGrounded && context.MoveSpeed <= 0.1f, 0, 0.1f),
            };
            locomotionStateMachine = new AnimationStateMachineNode(states, transitions, "Idle");
            var cachedLocomotion = new CachedPoseNode(locomotionStateMachine);
            var layered = new LayeredBlendPerBoneNode(
                cachedLocomotion,
                new LayeredAnimationInput(new ClipNode(config.Stop.AnimationClip), new AvatarMask(), context => context.OverlayWeight, false, "UpperBodyAction"),
                new LayeredAnimationInput(new ClipNode(config.Idle.AnimationClip), new AvatarMask(), context => context.LeftHandIkWeight, true, "AimAdditive"));

            IAnimationPlayableNode root = layered;
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips != null)
            {
                inertializationNode = new InertializationNode(root, hips);
                root = inertializationNode;
                locomotionStateMachine.StatePhaseChanged += OnStatePhaseChanged;
            }

            Transform spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            Transform chest = animator.GetBoneTransform(HumanBodyBones.UpperChest)
                ?? animator.GetBoneTransform(HumanBodyBones.Chest);
            observerSpine = chest != null
                ? spine
                : animator.GetBoneTransform(HumanBodyBones.Neck) ?? spine;
            observerChest = chest ?? animator.GetBoneTransform(HumanBodyBones.Head);

            Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            if (leftHand != null)
            {
                leftHandTarget = new GameObject("[LeftHandIkTarget]");
                leftHandTarget.transform.SetParent(animator.transform, true);
                leftHandTarget.transform.SetPositionAndRotation(leftHand.position, leftHand.rotation);
                root = new LeftHandIkNode(root, leftHand, leftHandTarget.transform);
            }

            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (leftFoot != null && rightFoot != null)
            {
                root = new FootIkNode(root, animator, leftFoot, rightFoot);
            }

            root = new RootDeltaNode(root);
            output = new OutputNode(root, "CharacterAnimationGraph");
            output.Initialize(animator);
        }

        public AnimationGraphContext Context => output.Context;
        public string CurrentLocomotionState => locomotionStateMachine.CurrentState;
        public bool IsInitialized => output.IsInitialized;

        public void Update(float deltaTime)
        {
            output.Update(deltaTime);
            ApplyObserverAim();
        }

        public AnimationGraphDebugSnapshot GetDebugSnapshot() => output.GetGraphDebugSnapshot();

        public void Dispose()
        {
            locomotionStateMachine.StatePhaseChanged -= OnStatePhaseChanged;
            output.Destroy();
            if (leftHandTarget != null)
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(leftHandTarget);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(leftHandTarget);
                }
            }
        }

        private void OnStatePhaseChanged(string state, LocomotionStatePhase phase)
        {
            if (phase == LocomotionStatePhase.Enter)
            {
                inertializationNode?.Request(0.12f);
            }
        }

        private void ApplyObserverAim()
        {
            AnimationGraphContext context = Context;
            if (observerSpine == null || context.ObserverAimWeight <= 0f)
            {
                return;
            }

            bool hasChest = observerChest != null && observerChest != observerSpine;
            float spineShare = hasChest ? 0.4f : 0.65f;
            ApplyObserverAimRotation(observerSpine, context, spineShare);
            if (hasChest)
            {
                ApplyObserverAimRotation(observerChest, context, 0.6f);
            }
        }

        private static void ApplyObserverAimRotation(
            Transform bone,
            AnimationGraphContext context,
            float share)
        {
            Quaternion poseRotation = bone.localRotation;
            Quaternion aimOffset = Quaternion.Euler(
                -context.ObserverAimPitch * share,
                context.ObserverAimYawOffset * share,
                0f);
            Quaternion aimedRotation = poseRotation * aimOffset;
            bone.localRotation = Quaternion.Slerp(
                poseRotation,
                aimedRotation,
                Mathf.Clamp01(context.ObserverAimWeight));
        }

        private static ClipNode CreateOneShot(AnimationClip clip)
        {
            return new ClipNode(clip) { Loop = false };
        }
    }
}
