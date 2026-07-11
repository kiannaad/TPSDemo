using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CGame.Animation
{
    public sealed class RootDeltaNode : AnimationNodeBase
    {
        private readonly IAnimationPlayableNode inputNode;
        private readonly Func<AnimationGraphContext, float> weightGetter;
        private AnimationScriptPlayable scriptPlayable;

        public RootDeltaNode(IAnimationPlayableNode inputNode, Func<AnimationGraphContext, float> weightGetter = null)
        {
            this.inputNode = inputNode ?? throw new ArgumentNullException(nameof(inputNode));
            this.weightGetter = weightGetter;
        }

        public AnimationScriptPlayable ScriptPlayable => scriptPlayable;

        public override void Update(AnimationGraphContext context, float deltaTime)
        {
            RootMotionCaptureJob job = scriptPlayable.GetJobData<RootMotionCaptureJob>();
            float weight = weightGetter != null ? Mathf.Clamp01(weightGetter(context)) : 1f;
            context.AccumulateRootMotionDelta(new AnimationRootMotionDelta(job.PositionDelta, job.RotationDelta, weight));
            job.PositionDelta = Vector3.zero;
            job.RotationDelta = Quaternion.identity;
            scriptPlayable.SetJobData(job);
            inputNode.Update(context, deltaTime);
        }

        public override AnimationPoseHandle Evaluate(AnimationGraphContext context)
        {
            inputNode.Evaluate(context);
            return new AnimationPoseHandle(scriptPlayable, 1f, context.EvaluateFrameId, nameof(RootDeltaNode));
        }

        public override AnimationNodeDebugSnapshot GetDebugSnapshot()
        {
            return new AnimationNodeDebugSnapshot(nameof(RootDeltaNode), scriptPlayable.IsValid(), 1f, 1);
        }

        protected override void OnInitialize(AnimationGraphContext context)
        {
            inputNode.Initialize(context);
            var job = new RootMotionCaptureJob { RotationDelta = Quaternion.identity };
            scriptPlayable = AnimationScriptPlayable.Create(context.Graph, job, 1);
            context.Graph.Connect(inputNode.Evaluate(context).Playable, 0, scriptPlayable, 0);
            scriptPlayable.SetInputWeight(0, 1f);
            context.RootMotionDelta = AnimationRootMotionDelta.None;
        }

        protected override void OnDestroy()
        {
            inputNode.Destroy();
        }
    }
}
