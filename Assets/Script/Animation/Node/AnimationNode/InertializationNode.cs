using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CGame.Animation
{
    public sealed class InertializationNode : AnimationNodeBase
    {
        private readonly IAnimationPlayableNode inputNode;
        private readonly Transform boundTransform;
        private readonly float minWeight;
        private AnimationScriptPlayable scriptPlayable;
        private bool pendingRequest;
        private float requestedDuration;
        private float requestedWeight;

        public InertializationNode(
            IAnimationPlayableNode inputNode,
            Transform boundTransform,
            float minWeight = 0.01f)
        {
            this.inputNode = inputNode ?? throw new ArgumentNullException(nameof(inputNode));
            this.boundTransform = boundTransform ?? throw new ArgumentNullException(nameof(boundTransform));
            this.minWeight = Mathf.Max(0f, minWeight);
        }

        public bool Enabled { get; set; } = true;
        public AnimationScriptPlayable ScriptPlayable => scriptPlayable;
        public bool IsActive => scriptPlayable.IsValid() && scriptPlayable.GetJobData<InertializationJob>().IsActive;

        public void Request(float duration, float weight = 1f)
        {
            pendingRequest = true;
            requestedDuration = Mathf.Max(0f, duration);
            requestedWeight = Mathf.Clamp01(weight);
        }

        public override void Update(AnimationGraphContext context, float deltaTime)
        {
            inputNode.Update(context, deltaTime);
            InertializationJob job = scriptPlayable.GetJobData<InertializationJob>();
            job.Enabled = Enabled;
            if (pendingRequest)
            {
                pendingRequest = false;
                job.Duration = requestedDuration;
                job.Weight = requestedWeight;
                job.Trigger = true;
            }

            scriptPlayable.SetJobData(job);
        }

        public override AnimationPoseHandle Evaluate(AnimationGraphContext context)
        {
            inputNode.Evaluate(context);
            return new AnimationPoseHandle(scriptPlayable, 1f, context.EvaluateFrameId, nameof(InertializationNode));
        }

        public override AnimationNodeDebugSnapshot GetDebugSnapshot()
        {
            return new AnimationNodeDebugSnapshot(nameof(InertializationNode), scriptPlayable.IsValid(), IsActive ? 1f : 0f, 1);
        }

        protected override void OnInitialize(AnimationGraphContext context)
        {
            inputNode.Initialize(context);
            var job = new InertializationJob
            {
                TransformHandle = context.Animator.BindStreamTransform(boundTransform),
                PreviousOutputRotation = Quaternion.identity,
                RotationOffset = Quaternion.identity,
                Enabled = Enabled,
                MinWeight = minWeight,
                Weight = 1f,
            };
            scriptPlayable = AnimationScriptPlayable.Create(context.Graph, job, 1);
            context.Graph.Connect(inputNode.Evaluate(context).Playable, 0, scriptPlayable, 0);
            scriptPlayable.SetInputWeight(0, 1f);
        }

        protected override void OnDestroy()
        {
            inputNode.Destroy();
        }
    }
}
