using UnityEngine.Playables;

namespace CGame.Animation
{
    public abstract class AnimationNodeBase : IAnimationPlayableNode
    {
        public bool IsInitialized { get; private set; }

        public void Initialize(AnimationGraphContext context)
        {
            if (IsInitialized)
            {
                return;
            }

            OnInitialize(context);
            IsInitialized = true;
        }

        public virtual void Update(AnimationGraphContext context, float deltaTime)
        {
        }

        public abstract AnimationPoseHandle Evaluate(AnimationGraphContext context);

        public abstract AnimationNodeDebugSnapshot GetDebugSnapshot();

        public void Destroy()
        {
            if (!IsInitialized)
            {
                return;
            }

            OnDestroy();
            IsInitialized = false;
        }

        protected abstract void OnInitialize(AnimationGraphContext context);

        protected virtual void OnDestroy()
        {
        }
    }
}
