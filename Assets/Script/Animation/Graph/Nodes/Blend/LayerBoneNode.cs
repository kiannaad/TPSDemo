using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CGame.Animation
{
    public sealed class LayerBoneNode : AnimationNodeBase
    {
        private readonly IAnimationPlayableNode baseNode;
        private readonly IAnimationPlayableNode overlayNode;
        private readonly AvatarMask overlayMask;
        private readonly Func<AnimationGraphContext, float> overlayWeightGetter;
        private AnimationLayerMixerPlayable layerMixerPlayable;
        private float overlayWeight;

        public LayerBoneNode(
            IAnimationPlayableNode baseNode,
            IAnimationPlayableNode overlayNode,
            AvatarMask overlayMask,
            Func<AnimationGraphContext, float> overlayWeightGetter = null)
        {
            this.baseNode = baseNode;
            this.overlayNode = overlayNode;
            this.overlayMask = overlayMask;
            this.overlayWeightGetter = overlayWeightGetter;
        }

        public AnimationLayerMixerPlayable LayerMixerPlayable => layerMixerPlayable;
        public float OverlayWeight => overlayWeight;

        public override void Update(AnimationGraphContext context, float deltaTime)
        {
            baseNode?.Update(context, deltaTime);
            overlayNode?.Update(context, deltaTime);
        }

        public override AnimationPoseHandle Evaluate(AnimationGraphContext context)
        {
            baseNode?.Evaluate(context);
            overlayNode?.Evaluate(context);

            overlayWeight = overlayWeightGetter != null
                ? Mathf.Clamp01(overlayWeightGetter(context))
                : Mathf.Clamp01(context.OverlayWeight);

            if (layerMixerPlayable.IsValid())
            {
                layerMixerPlayable.SetInputWeight(0, 1f);
                layerMixerPlayable.SetInputWeight(1, overlayWeight);
            }

            return new AnimationPoseHandle(layerMixerPlayable, 1f, context.EvaluateFrameId, nameof(LayerBoneNode));
        }

        public override AnimationNodeDebugSnapshot GetDebugSnapshot()
        {
            return new AnimationNodeDebugSnapshot(nameof(LayerBoneNode), layerMixerPlayable.IsValid(), overlayWeight, 2);
        }

        protected override void OnInitialize(AnimationGraphContext context)
        {
            if (baseNode == null)
            {
                throw new InvalidOperationException("LayerBoneNode requires a base node.");
            }

            layerMixerPlayable = AnimationLayerMixerPlayable.Create(context.Graph, 2);
            ConnectInput(context, baseNode, 0);
            ConnectInput(context, overlayNode, 1);

            layerMixerPlayable.SetInputWeight(0, 1f);
            layerMixerPlayable.SetInputWeight(1, 0f);
            if (overlayMask != null)
            {
                layerMixerPlayable.SetLayerMaskFromAvatarMask(1, overlayMask);
            }
        }

        protected override void OnDestroy()
        {
            baseNode?.Destroy();
            overlayNode?.Destroy();
        }

        private void ConnectInput(AnimationGraphContext context, IAnimationPlayableNode node, int inputIndex)
        {
            if (node == null)
            {
                return;
            }

            node.Initialize(context);
            Playable playable = node.Evaluate(context);
            if (playable.IsValid())
            {
                context.Graph.Connect(playable, 0, layerMixerPlayable, inputIndex);
            }
        }
    }
}
