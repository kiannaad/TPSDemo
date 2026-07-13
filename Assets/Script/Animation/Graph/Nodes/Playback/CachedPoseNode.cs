using System;

namespace CGame.Animation
{
    public sealed class CachedPoseNode : AnimationNodeBase
    {
        private readonly IAnimationPlayableNode inputNode;
        private AnimationPoseHandle cachedPose;
        private long cachedFrameId = -1;

        public CachedPoseNode(IAnimationPlayableNode inputNode)
        {
            this.inputNode = inputNode ?? throw new ArgumentNullException(nameof(inputNode));
        }

        public long CachedFrameId => cachedFrameId;
        public AnimationPoseHandle CachedPose => cachedPose;

        public override void Update(AnimationGraphContext context, float deltaTime)
        {
            inputNode.Update(context, deltaTime);
        }

        public override AnimationPoseHandle Evaluate(AnimationGraphContext context)
        {
            if (cachedFrameId != context.EvaluateFrameId)
            {
                cachedPose = inputNode.Evaluate(context);
                cachedFrameId = context.EvaluateFrameId;
            }

            return cachedPose;
        }

        public override AnimationNodeDebugSnapshot GetDebugSnapshot()
        {
            return new AnimationNodeDebugSnapshot(nameof(CachedPoseNode), cachedPose.IsValid, cachedPose.Weight, 1);
        }

        protected override void OnInitialize(AnimationGraphContext context)
        {
            inputNode.Initialize(context);
        }

        protected override void OnDestroy()
        {
            inputNode.Destroy();
            cachedPose = default;
            cachedFrameId = -1;
        }
    }
}
