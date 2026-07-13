using System;
using Animancer;
using UnityEngine;

namespace CGame.Animation
{
    public static class AnimancerComponentAnimationAssetExtensions
    {
        public static AnimancerState Play(this AnimancerComponent animancer, IAnimationAsset animationAsset)
        {
            if (animancer == null)
            {
                throw new ArgumentNullException(nameof(animancer));
            }

            if (animationAsset == null)
            {
                return null;
            }

            return animancer.Play(animationAsset.CreateTransition());
        }

        public static AnimationNotifyRuntime CreateNotifyRuntime(
            this AnimancerComponent animancer,
            UnityEngine.Object owner,
            AnimationClipAsset clipAsset,
            AnimancerState state)
        {
            if (animancer == null)
            {
                throw new ArgumentNullException(nameof(animancer));
            }

            if (clipAsset == null)
            {
                return null;
            }

            return new AnimationNotifyRuntime(owner, clipAsset, state, animancer);
        }
    }
}
