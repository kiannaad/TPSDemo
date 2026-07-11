using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CGame.Animation
{
    public sealed class AdditiveNode : AnimationNodeBase
    {
        private readonly IAnimationPlayableNode baseNode;
        private readonly IAnimationPlayableNode additiveNode;
        private readonly AvatarMask mask;
        private readonly Func<AnimationGraphContext, float> weightGetter;
        private AnimationLayerMixerPlayable mixerPlayable;

        public AdditiveNode(
            IAnimationPlayableNode baseNode,
            IAnimationPlayableNode additiveNode,
            AvatarMask mask = null,
            Func<AnimationGraphContext, float> weightGetter = null)
        {
            this.baseNode = baseNode ?? throw new ArgumentNullException(nameof(baseNode));
            this.additiveNode = additiveNode ?? throw new ArgumentNullException(nameof(additiveNode));
            this.mask = mask;
            this.weightGetter = weightGetter;
        }

        public AnimationLayerMixerPlayable MixerPlayable => mixerPlayable;
        public float Weight { get; private set; }

        public override void Update(AnimationGraphContext context, float deltaTime)
        {
            baseNode.Update(context, deltaTime);
            additiveNode.Update(context, deltaTime);
        }

        public override AnimationPoseHandle Evaluate(AnimationGraphContext context)
        {
            baseNode.Evaluate(context);
            additiveNode.Evaluate(context);
            Weight = Mathf.Clamp01(weightGetter != null ? weightGetter(context) : 1f);
            mixerPlayable.SetInputWeight(0, 1f);
            mixerPlayable.SetInputWeight(1, Weight);
            return new AnimationPoseHandle(mixerPlayable, 1f, context.EvaluateFrameId, nameof(AdditiveNode));
        }

        public override AnimationNodeDebugSnapshot GetDebugSnapshot()
        {
            return new AnimationNodeDebugSnapshot(nameof(AdditiveNode), mixerPlayable.IsValid(), Weight, 2);
        }

        protected override void OnInitialize(AnimationGraphContext context)
        {
            mixerPlayable = AnimationLayerMixerPlayable.Create(context.Graph, 2);
            ConnectNode(context, baseNode, 0);
            ConnectNode(context, additiveNode, 1);
            mixerPlayable.SetInputWeight(0, 1f);
            mixerPlayable.SetInputWeight(1, 0f);
            mixerPlayable.SetLayerAdditive(1, true);
            if (mask != null)
            {
                mixerPlayable.SetLayerMaskFromAvatarMask(1, mask);
            }
        }

        protected override void OnDestroy()
        {
            baseNode.Destroy();
            additiveNode.Destroy();
        }

        private void ConnectNode(AnimationGraphContext context, IAnimationPlayableNode node, int inputIndex)
        {
            node.Initialize(context);
            AnimationPoseHandle pose = node.Evaluate(context);
            context.Graph.Connect(pose.Playable, 0, mixerPlayable, inputIndex);
        }
    }
}
