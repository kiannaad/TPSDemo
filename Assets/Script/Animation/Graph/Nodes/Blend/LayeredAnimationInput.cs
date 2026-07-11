using System;
using UnityEngine;

namespace CGame.Animation
{
    public sealed class LayeredAnimationInput
    {
        private readonly Func<AnimationGraphContext, float> weightGetter;

        public LayeredAnimationInput(
            IAnimationPlayableNode node,
            AvatarMask mask,
            Func<AnimationGraphContext, float> weightGetter = null,
            bool isAdditive = false,
            string name = null)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
            Mask = mask;
            this.weightGetter = weightGetter;
            IsAdditive = isAdditive;
            Name = string.IsNullOrWhiteSpace(name) ? node.GetType().Name : name;
        }

        public IAnimationPlayableNode Node { get; }
        public AvatarMask Mask { get; }
        public bool IsAdditive { get; }
        public string Name { get; }

        public float GetWeight(AnimationGraphContext context)
        {
            return Mathf.Clamp01(weightGetter != null ? weightGetter(context) : 1f);
        }
    }
}
