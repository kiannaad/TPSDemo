using Animancer;
using UnityEngine;

namespace CGame.Animation
{
    public class AnimationClipAsset : AnimationAssetBase
    {
        [SerializeField] private AnimationClip animationClip;
        [SerializeField, Min(0f)] private float fadeDuration = AnimancerGraph.DefaultFadeDuration;
        [SerializeField] private float speed = 1f;
        [SerializeField] private bool overrideNormalizedStartTime;
        [SerializeField] private float normalizedStartTime;

        public AnimationClip AnimationClip => animationClip;

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

        public bool TryInitialize(AnimationClip clip)
        {
            if (animationClip != null || clip == null)
            {
                return false;
            }

            animationClip = clip;
            return true;
        }

        public ClipTransition CreateClipTransition()
        {
            return CreateClipTransition(1f);
        }

        public ClipTransition CreateClipTransition(float speedMultiplier)
        {
            var transition = new ClipTransition
            {
                Clip = animationClip,
                FadeDuration = fadeDuration,
                Speed = speed * speedMultiplier,
            };

            if (overrideNormalizedStartTime)
            {
                transition.NormalizedStartTime = normalizedStartTime;
            }

            return transition;
        }
    }
}
