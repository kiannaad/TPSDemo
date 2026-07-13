using UnityEngine.Playables;

namespace CGame.Animation
{
    public interface IAnimationPlayableNode
    {
        bool IsInitialized { get; }
        void Initialize(AnimationGraphContext context);
        void Update(AnimationGraphContext context, float deltaTime);
        AnimationPoseHandle Evaluate(AnimationGraphContext context);
        AnimationNodeDebugSnapshot GetDebugSnapshot();
        void Destroy();
    }
}
