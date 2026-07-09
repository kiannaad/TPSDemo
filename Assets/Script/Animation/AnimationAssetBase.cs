using System;
using Animancer;
using UnityEngine;

namespace CGame.Animation
{
    public abstract class AnimationAssetBase : ScriptableObject, IAnimationAsset
    {
        public abstract AnimationClip MainClip { get; }
        public abstract bool IsValid { get; }

        public abstract ITransition CreateTransition();

        public AnimancerState Play(AnimancerComponent animancer)
        {
            return animancer.Play(this);
        }
    }
}
