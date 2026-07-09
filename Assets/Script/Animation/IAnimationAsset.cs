using Animancer;
using UnityEngine;

namespace CGame.Animation
{
    public interface IAnimationAsset
    {
        AnimationClip MainClip { get; }
        bool IsValid { get; }

        ITransition CreateTransition();
        AnimancerState Play(AnimancerComponent animancer);
    }
}
