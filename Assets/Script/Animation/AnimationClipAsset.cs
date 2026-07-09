using Animancer;
using UnityEngine;

namespace CGame.Animation
{
    [CreateAssetMenu(fileName = "AnimationClipAsset", menuName = "CGame/Animation/Animation Clip Asset")]
    public class AnimationClipAsset : AnimationAssetBase
    {
        [SerializeField] private AnimationClip animationClip;
        [SerializeField, Min(0f)] private float fadeDuration = AnimancerGraph.DefaultFadeDuration;
        [SerializeField] private float speed = 1f;
        [SerializeField] private bool overrideNormalizedStartTime;
        [SerializeField] private float normalizedStartTime;

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

        public bool OverrideNormalizedStartTime
        {
            get => overrideNormalizedStartTime;
            set => overrideNormalizedStartTime = value;
        }

        public float NormalizedStartTime
        {
            get => normalizedStartTime;
            set => normalizedStartTime = value;
        }

        public override AnimationClip MainClip => animationClip;
        public override bool IsValid => animationClip != null && !animationClip.legacy;

        public override ITransition CreateTransition()
        {
            return CreateClipTransition();
        }

        public ClipTransition CreateClipTransition()
        {
            var transition = new ClipTransition
            {
                Clip = animationClip,
                FadeDuration = fadeDuration,
                Speed = speed,
            };

            if (overrideNormalizedStartTime)
            {
                transition.NormalizedStartTime = normalizedStartTime;
            }

            return transition;
        }
    }
}
