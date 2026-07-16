using UnityEngine;

namespace CGame.Animation
{
    public class AnimationClipAsset : AnimationAssetBase
    {
        [SerializeField] private AnimationClip animationClip;
        [SerializeField, Min(0f)] private float fadeDuration = 0.25f;
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

        public bool TryInitialize(AnimationClip clip)
        {
            if (animationClip != null || clip == null)
            {
                return false;
            }

            animationClip = clip;
            return true;
        }

    }
}
