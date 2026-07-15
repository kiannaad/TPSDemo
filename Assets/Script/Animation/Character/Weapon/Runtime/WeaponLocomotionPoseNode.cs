using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CGame.Animation
{
    public sealed class WeaponLocomotionPoseNode : AnimationNodeBase
    {
        private readonly Func<string> stateGetter;
        private readonly IAnimationPlayableNode[] nodes = new IAnimationPlayableNode[3];
        private readonly bool[] available = new bool[3];
        private readonly float blendDuration;
        private AnimationMixerPlayable mixerPlayable;
        private int fallbackIndex;
        private int currentIndex = -1;
        private int targetIndex = -1;
        private float blendElapsed;

        public WeaponLocomotionPoseNode(WeaponAnimationDefinition definition, Func<string> stateGetter)
        {
            if (definition == null || !definition.IsValid)
            {
                throw new ArgumentException("A valid weapon animation definition is required.", nameof(definition));
            }

            this.stateGetter = stateGetter ?? throw new ArgumentNullException(nameof(stateGetter));
            blendDuration = definition.BlendDuration;
            nodes[0] = CreateClipNode(definition.Idle);
            nodes[1] = CreateMoveNode(definition.Walk, definition.Run);
            nodes[2] = CreateClipNode(definition.Stop);
            for (int i = 0; i < nodes.Length; i++)
            {
                available[i] = nodes[i] != null;
                if (available[i] && currentIndex < 0)
                {
                    currentIndex = i;
                    targetIndex = i;
                    fallbackIndex = i;
                }
            }
        }

        public bool IsPoseAvailable { get; private set; }
        public string SelectedLocomotionState { get; private set; } = string.Empty;

        public override void Update(AnimationGraphContext context, float deltaTime)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i]?.Update(context, deltaTime);
            }

            SelectedLocomotionState = stateGetter() ?? string.Empty;
            int desiredIndex = GetStateIndex(SelectedLocomotionState);
            IsPoseAvailable = desiredIndex >= 0 && available[desiredIndex];
            if (!IsPoseAvailable)
            {
                return;
            }

            if (desiredIndex != targetIndex)
            {
                currentIndex = targetIndex >= 0 ? targetIndex : desiredIndex;
                targetIndex = desiredIndex;
                blendElapsed = 0f;
            }

            blendElapsed = Mathf.Min(blendDuration, blendElapsed + Mathf.Max(0f, deltaTime));
        }

        public override AnimationPoseHandle Evaluate(AnimationGraphContext context)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i]?.Evaluate(context);
                mixerPlayable.SetInputWeight(i, 0f);
            }

            if (!IsPoseAvailable)
            {
                mixerPlayable.SetInputWeight(fallbackIndex, 1f);
            }
            else if (currentIndex != targetIndex && blendDuration > 0f && blendElapsed < blendDuration)
            {
                float progress = Mathf.Clamp01(blendElapsed / blendDuration);
                mixerPlayable.SetInputWeight(currentIndex, 1f - progress);
                mixerPlayable.SetInputWeight(targetIndex, progress);
            }
            else
            {
                currentIndex = targetIndex;
                mixerPlayable.SetInputWeight(targetIndex, 1f);
            }

            return new AnimationPoseHandle(mixerPlayable, 1f, context.EvaluateFrameId, nameof(WeaponLocomotionPoseNode));
        }

        public override AnimationNodeDebugSnapshot GetDebugSnapshot()
        {
            return new AnimationNodeDebugSnapshot(nameof(WeaponLocomotionPoseNode), mixerPlayable.IsValid(), IsPoseAvailable ? 1f : 0f, nodes.Length);
        }

        protected override void OnInitialize(AnimationGraphContext context)
        {
            mixerPlayable = AnimationMixerPlayable.Create(context.Graph, nodes.Length);
            for (int i = 0; i < nodes.Length; i++)
            {
                IAnimationPlayableNode node = nodes[i];
                if (node == null)
                {
                    continue;
                }

                node.Initialize(context);
                AnimationPoseHandle pose = node.Evaluate(context);
                context.Graph.Connect(pose.Playable, 0, mixerPlayable, i);
                mixerPlayable.SetInputWeight(i, i == fallbackIndex ? 1f : 0f);
            }
        }

        protected override void OnDestroy()
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i]?.Destroy();
            }
        }

        private static IAnimationPlayableNode CreateClipNode(AnimationClipAsset asset)
        {
            return asset != null && asset.IsValid ? new ClipNode(asset.AnimationClip) : null;
        }

        private static IAnimationPlayableNode CreateMoveNode(AnimationClipAsset walk, AnimationClipAsset run)
        {
            bool hasWalk = walk != null && walk.IsValid;
            bool hasRun = run != null && run.IsValid;
            if (hasWalk && hasRun)
            {
                return new Blend1DNode(new[]
                {
                    new Blend1DChild(new ClipNode(walk.AnimationClip), 1f),
                    new Blend1DChild(new ClipNode(run.AnimationClip), 3f),
                }, context => context.MoveSpeed);
            }

            if (hasWalk)
            {
                return new ClipNode(walk.AnimationClip);
            }

            return hasRun ? new ClipNode(run.AnimationClip) : null;
        }

        private static int GetStateIndex(string state)
        {
            switch (state)
            {
                case "Idle": return 0;
                case "Move": return 1;
                case "Stop": return 2;
                default: return -1;
            }
        }
    }
}
