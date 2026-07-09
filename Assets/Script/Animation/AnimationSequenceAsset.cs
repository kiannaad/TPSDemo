using Animancer;
using UnityEngine;

namespace CGame.Animation
{
    [CreateAssetMenu(fileName = "AnimationSequenceAsset", menuName = "CGame/Animation/Animation Sequence Asset")]
    public class AnimationSequenceAsset : AnimationAssetBase
    {
        [SerializeField] private AnimationClip animationClip;
        [SerializeField, Min(0f)] private float fadeDuration = AnimancerGraph.DefaultFadeDuration;
        [SerializeField] private float speed = 1f;

        public AnimationClip AnimationClip
        {
            get => animationClip;
            set => animationClip = value;
        }

        public float FadeDuration
        {
            get => fadeDuration;
            set => fadeDuration = Mathf.Max(0f, value);
        }

        public float Speed
        {
            get => speed;
            set => speed = value;
        }

        public override AnimationClip MainClip => animationClip;
        public override bool IsValid => animationClip != null && !animationClip.legacy;

        public override ITransition CreateTransition()
        {
            var transition = new ClipTransition
            {
                Clip = animationClip,
                FadeDuration = fadeDuration,
                Speed = speed,
            };

            return transition;
        }
    }
}
