using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CGame.Animation
{
    public sealed class Blend1DNode : AnimationNodeBase
    {
        private readonly Blend1DChild[] children;
        private readonly Func<AnimationGraphContext, float> parameterGetter;
        private AnimationMixerPlayable mixerPlayable;
        private float parameter;

        public Blend1DNode(Blend1DChild[] children, Func<AnimationGraphContext, float> parameterGetter)
        {
            this.children = children ?? Array.Empty<Blend1DChild>();
            this.parameterGetter = parameterGetter;
        }

        public AnimationMixerPlayable MixerPlayable => mixerPlayable;
        public float Parameter => parameter;

        public override void Update(AnimationGraphContext context, float deltaTime)
        {
            for (int i = 0; i < children.Length; i++)
            {
                children[i].Node?.Update(context, deltaTime);
            }
        }

        public override AnimationPoseHandle Evaluate(AnimationGraphContext context)
        {
            parameter = parameterGetter != null ? parameterGetter(context) : 0f;
            ApplyWeights(parameter);
            return new AnimationPoseHandle(mixerPlayable, 1f, context.EvaluateFrameId, nameof(Blend1DNode));
        }

        public override AnimationNodeDebugSnapshot GetDebugSnapshot()
        {
            return new AnimationNodeDebugSnapshot(nameof(Blend1DNode), mixerPlayable.IsValid(), 1f, children.Length);
        }

        protected override void OnInitialize(AnimationGraphContext context)
        {
            mixerPlayable = AnimationMixerPlayable.Create(context.Graph, children.Length);
            for (int i = 0; i < children.Length; i++)
            {
                IAnimationPlayableNode childNode = children[i].Node;
                if (childNode == null)
                {
                    continue;
                }

                childNode.Initialize(context);
                Playable childPlayable = childNode.Evaluate(context);
                if (childPlayable.IsValid())
                {
                    context.Graph.Connect(childPlayable, 0, mixerPlayable, i);
                }
            }
        }

        protected override void OnDestroy()
        {
            for (int i = 0; i < children.Length; i++)
            {
                children[i].Node?.Destroy();
            }
        }

        private void ApplyWeights(float value)
        {
            if (!mixerPlayable.IsValid())
            {
                return;
            }

            if (children.Length == 0)
            {
                return;
            }

            if (children.Length == 1)
            {
                mixerPlayable.SetInputWeight(0, 1f);
                return;
            }

            for (int i = 0; i < children.Length; i++)
            {
                mixerPlayable.SetInputWeight(i, 0f);
            }

            if (value <= children[0].Threshold)
            {
                mixerPlayable.SetInputWeight(0, 1f);
                return;
            }

            int lastIndex = children.Length - 1;
            if (value >= children[lastIndex].Threshold)
            {
                mixerPlayable.SetInputWeight(lastIndex, 1f);
                return;
            }

            for (int i = 0; i < lastIndex; i++)
            {
                float from = children[i].Threshold;
                float to = children[i + 1].Threshold;
                if (value < from || value > to)
                {
                    continue;
                }

                float range = Mathf.Max(0.0001f, to - from);
                float alpha = Mathf.Clamp01((value - from) / range);
                mixerPlayable.SetInputWeight(i, 1f - alpha);
                mixerPlayable.SetInputWeight(i + 1, alpha);
                return;
            }
        }
    }
}
