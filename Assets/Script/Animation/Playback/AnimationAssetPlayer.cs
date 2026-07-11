using Animancer;
using UnityEngine;

namespace CGame.Animation
{
    public class AnimationAssetPlayer : MonoBehaviour
    {
        [SerializeField] private AnimancerComponent animancer;
        [SerializeField] private AnimationAssetBase animationAsset;
        [SerializeField] private bool playOnEnable = true;
        private AnimationNotifyRuntime notifyRuntime;

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

        public AnimationNotifyRuntime NotifyRuntime => notifyRuntime;

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

        private void OnDisable()
        {
            EndNotifyRuntime(AnimationNotifyEndReason.OwnerDisabled);
        }

        private void OnDestroy()
        {
            EndNotifyRuntime(AnimationNotifyEndReason.Interrupted);
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

            EndNotifyRuntime(AnimationNotifyEndReason.Interrupted);

            AnimancerState state = animancer.Play(animationAsset);
            if (animationAsset is AnimationClipAsset clipAsset)
            {
                notifyRuntime = animancer.CreateNotifyRuntime(gameObject, clipAsset, state);
            }

            return state;
        }

        public void EvaluateWithNotify(float deltaTime)
        {
            notifyRuntime?.EvaluateWithNotify(deltaTime);
        }

#if UNITY_INCLUDE_TESTS
        public void CaptureNotifyBeforeEvaluateForTesting()
        {
            notifyRuntime?.CaptureBeforeEvaluate();
        }

        public void DispatchNotifyAfterEvaluateForTesting(float deltaTime)
        {
            notifyRuntime?.DispatchAfterEvaluate(deltaTime);
        }
#endif

        private void EndNotifyRuntime(AnimationNotifyEndReason reason)
        {
            notifyRuntime?.EndAll(reason);
            notifyRuntime = null;
        }
    }
}
