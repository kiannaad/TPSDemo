using Animancer;
using UnityEngine;

namespace CGame.Animation
{
    public class AnimationAssetPlayer : MonoBehaviour
    {
        [SerializeField] private AnimancerComponent animancer;
        [SerializeField] private AnimationAssetBase animationAsset;
        [SerializeField] private bool playOnEnable = true;

        public AnimancerComponent Animancer
        {
            get => animancer;
            set => animancer = value;
        }

        public AnimationAssetBase AnimationAsset
        {
            get => animationAsset;
            set => animationAsset = value;
        }

        public bool PlayOnEnable
        {
            get => playOnEnable;
            set => playOnEnable = value;
        }

        private void Reset()
        {
            animancer = GetComponent<AnimancerComponent>();
        }

        private void OnEnable()
        {
            if (playOnEnable && animationAsset != null)
            {
                Play();
            }
        }

        public AnimancerState Play()
        {
            if (animancer == null)
            {
                animancer = GetComponent<AnimancerComponent>();
            }

            if (animationAsset == null)
            {
                return null;
            }

            return animancer.Play(animationAsset);
        }
    }
}
