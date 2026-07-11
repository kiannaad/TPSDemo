using System;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CGame.Animation
{
    public sealed class LayeredBlendPerBoneNode : AnimationNodeBase
    {
        private readonly IAnimationPlayableNode baseNode;
        private readonly LayeredAnimationInput[] layers;
        private AnimationLayerMixerPlayable mixerPlayable;

        public LayeredBlendPerBoneNode(IAnimationPlayableNode baseNode, params LayeredAnimationInput[] layers)
        {
            this.baseNode = baseNode ?? throw new ArgumentNullException(nameof(baseNode));
            this.layers = layers ?? Array.Empty<LayeredAnimationInput>();
        }

        public AnimationLayerMixerPlayable MixerPlayable => mixerPlayable;
        public int LayerCount => layers.Length;

        public override void Update(AnimationGraphContext context, float deltaTime)
        {
            baseNode.Update(context, deltaTime);
            for (int i = 0; i < layers.Length; i++)
            {
                layers[i].Node.Update(context, deltaTime);
            }
        }

        public override AnimationPoseHandle Evaluate(AnimationGraphContext context)
        {
            baseNode.Evaluate(context);
            mixerPlayable.SetInputWeight(0, 1f);
            for (int i = 0; i < layers.Length; i++)
            {
                LayeredAnimationInput layer = layers[i];
                layer.Node.Evaluate(context);
                mixerPlayable.SetInputWeight(i + 1, layer.GetWeight(context));
            }

            return new AnimationPoseHandle(mixerPlayable, 1f, context.EvaluateFrameId, nameof(LayeredBlendPerBoneNode));
        }

        public override AnimationNodeDebugSnapshot GetDebugSnapshot()
        {
            return new AnimationNodeDebugSnapshot(nameof(LayeredBlendPerBoneNode), mixerPlayable.IsValid(), 1f, layers.Length + 1);
        }

        protected override void OnInitialize(AnimationGraphContext context)
        {
            mixerPlayable = AnimationLayerMixerPlayable.Create(context.Graph, layers.Length + 1);
            ConnectNode(context, baseNode, 0);
            mixerPlayable.SetInputWeight(0, 1f);
            for (int i = 0; i < layers.Length; i++)
            {
                LayeredAnimationInput layer = layers[i] ?? throw new InvalidOperationException("Layer entries cannot be null.");
                int inputIndex = i + 1;
                ConnectNode(context, layer.Node, inputIndex);
                if (layer.Mask != null)
                {
                    mixerPlayable.SetLayerMaskFromAvatarMask((uint)inputIndex, layer.Mask);
                }

                mixerPlayable.SetLayerAdditive((uint)inputIndex, layer.IsAdditive);
                mixerPlayable.SetInputWeight(inputIndex, 0f);
            }
        }

        protected override void OnDestroy()
        {
            baseNode.Destroy();
            for (int i = 0; i < layers.Length; i++)
            {
                layers[i]?.Node.Destroy();
            }
        }

        private void ConnectNode(AnimationGraphContext context, IAnimationPlayableNode node, int inputIndex)
        {
            node.Initialize(context);
            AnimationPoseHandle pose = node.Evaluate(context);
            if (!pose.Playable.IsValid())
            {
                throw new InvalidOperationException($"Layer input {inputIndex} produced an invalid playable.");
            }

            context.Graph.Connect(pose.Playable, 0, mixerPlayable, inputIndex);
        }
    }
}
