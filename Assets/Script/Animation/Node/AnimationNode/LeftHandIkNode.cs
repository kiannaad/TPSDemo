using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CGame.Animation
{
    public sealed class LeftHandIkNode : AnimationNodeBase
    {
        private readonly IAnimationPlayableNode inputNode;
        private readonly Transform leftHand;
        private readonly Transform target;
        private AnimationScriptPlayable scriptPlayable;

        public LeftHandIkNode(IAnimationPlayableNode inputNode, Transform leftHand, Transform target)
        {
            this.inputNode = inputNode ?? throw new ArgumentNullException(nameof(inputNode));
            this.leftHand = leftHand ?? throw new ArgumentNullException(nameof(leftHand));
            this.target = target ?? throw new ArgumentNullException(nameof(target));
        }

        public AnimationScriptPlayable ScriptPlayable => scriptPlayable;

        public override void Update(AnimationGraphContext context, float deltaTime)
        {
            inputNode.Update(context, deltaTime);
            LeftHandIkJob job = scriptPlayable.GetJobData<LeftHandIkJob>();
            job.TargetPosition = target.position;
            job.TargetRotation = target.rotation;
            job.Weight = Mathf.Clamp01(context.LeftHandIkWeight);
            scriptPlayable.SetJobData(job);
        }

        public override AnimationPoseHandle Evaluate(AnimationGraphContext context)
        {
            inputNode.Evaluate(context);
            return new AnimationPoseHandle(scriptPlayable, 1f, context.EvaluateFrameId, nameof(LeftHandIkNode));
        }

        public override AnimationNodeDebugSnapshot GetDebugSnapshot()
        {
            return new AnimationNodeDebugSnapshot(nameof(LeftHandIkNode), scriptPlayable.IsValid(), contextWeight, 1);
        }

        protected override void OnInitialize(AnimationGraphContext context)
        {
            inputNode.Initialize(context);
            var job = new LeftHandIkJob
            {
                LeftHand = context.Animator.BindStreamTransform(leftHand),
                TargetPosition = target.position,
                TargetRotation = target.rotation,
            };
            scriptPlayable = AnimationScriptPlayable.Create(context.Graph, job, 1);
            context.Graph.Connect(inputNode.Evaluate(context).Playable, 0, scriptPlayable, 0);
            scriptPlayable.SetInputWeight(0, 1f);
        }

        protected override void OnDestroy()
        {
            inputNode.Destroy();
        }

        private float contextWeight => scriptPlayable.IsValid()
            ? scriptPlayable.GetJobData<LeftHandIkJob>().Weight
            : 0f;
    }
}
