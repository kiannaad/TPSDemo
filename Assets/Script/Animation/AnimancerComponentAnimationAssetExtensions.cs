using System;
using Animancer;

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
    }
}
