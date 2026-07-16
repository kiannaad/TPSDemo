using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CGame.Animation
{
    public class AnimationAssetPlayer : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private AnimationAssetBase animationAsset;
        [SerializeField] private bool playOnEnable = true;

        private PlayableGraph graph;
        private AnimationClipPlayable clipPlayable;
        private AnimationNotifyRuntime notifyRuntime;

        public Animator Animator
        {
            get => animator;
            set => animator = value;
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

        public bool IsPlaying => graph.IsValid() && graph.IsPlaying();
        public AnimationNotifyRuntime NotifyRuntime => notifyRuntime;

        private void Reset()
        {
            animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            if (playOnEnable && animationAsset != null)
            {
                Play();
            }
        }

        private void Update()
        {
            EvaluateWithNotify(Time.deltaTime);
        }

        private void OnDisable()
        {
            Stop(AnimationNotifyEndReason.OwnerDisabled);
        }

        private void OnDestroy()
        {
            Stop(AnimationNotifyEndReason.Interrupted);
        }

        public bool Play()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animator == null || animationAsset == null || !animationAsset.IsValid || animationAsset.MainClip == null)
            {
                return false;
            }

            Stop(AnimationNotifyEndReason.Interrupted);

            graph = PlayableGraph.Create($"{name}.AnimationAssetPlayer");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            clipPlayable = AnimationClipPlayable.Create(graph, animationAsset.MainClip);
            clipPlayable.SetApplyFootIK(false);
            if (animationAsset is AnimationClipAsset clipAsset)
            {
                clipPlayable.SetSpeed(clipAsset.Speed);
                notifyRuntime = new AnimationNotifyRuntime(gameObject, clipAsset);
            }

            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Animation", animator);
            output.SetSourcePlayable(clipPlayable);
            graph.Play();
            return true;
        }

        public void EvaluateWithNotify(float deltaTime)
        {
            if (!graph.IsValid() || !graph.IsPlaying())
            {
                return;
            }

            float clampedDeltaTime = Mathf.Max(0f, deltaTime);
            graph.Evaluate(clampedDeltaTime);
            notifyRuntime?.Tick(Mathf.Max(0f, (float)clipPlayable.GetTime()), clampedDeltaTime, 1f);
        }

        public void Stop(AnimationNotifyEndReason reason = AnimationNotifyEndReason.Interrupted)
        {
            notifyRuntime?.EndAll(reason);
            notifyRuntime = null;

            if (graph.IsValid())
            {
                graph.Destroy();
            }

            clipPlayable = default;
        }
    }
}
