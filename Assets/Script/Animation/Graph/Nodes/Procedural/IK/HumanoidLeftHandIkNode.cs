using System;
using CGame;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CGame.Animation
{
    public sealed class HumanoidLeftHandIkNode : AnimationNodeBase
    {
        private readonly IAnimationPlayableNode inputNode;
        private AnimationScriptPlayable scriptPlayable;
        private WeaponPresentationBinding binding;
        private float currentWeight;
        private float smoothingTime = 0.08f;

        public HumanoidLeftHandIkNode(IAnimationPlayableNode inputNode)
        {
            this.inputNode = inputNode ?? throw new ArgumentNullException(nameof(inputNode));
        }

        public float CurrentWeight => currentWeight;
        public AnimationScriptPlayable ScriptPlayable => scriptPlayable;

        public void SetBinding(WeaponPresentationBinding binding, float smoothingTime)
        {
            this.binding = binding;
            this.smoothingTime = Mathf.Max(0f, smoothingTime);
        }

        public override void Update(AnimationGraphContext context, float deltaTime)
        {
            inputNode.Update(context, deltaTime);
            bool canConsume = binding != null && binding.CanConsume(context.ActiveWeaponGeneration);
            float targetWeight = canConsume ? Mathf.Clamp01(context.LeftHandIkWeight) : 0f;
            float blend = smoothingTime <= 0f ? 1f : 1f - Mathf.Exp(-Mathf.Max(0f, deltaTime) / smoothingTime);
            currentWeight = Mathf.Lerp(currentWeight, targetWeight, blend);
            HumanoidLeftHandIkJob job = scriptPlayable.GetJobData<HumanoidLeftHandIkJob>();
            job.Weight = currentWeight;
            if (canConsume)
            {
                job.TargetPosition = binding.LeftHandGrip.position;
                job.TargetRotation = binding.LeftHandGrip.rotation;
            }
            scriptPlayable.SetJobData(job);
        }

        public override AnimationPoseHandle Evaluate(AnimationGraphContext context)
        {
            inputNode.Evaluate(context);
            return new AnimationPoseHandle(scriptPlayable, 1f, context.EvaluateFrameId, nameof(HumanoidLeftHandIkNode));
        }

        public override AnimationNodeDebugSnapshot GetDebugSnapshot()
        {
            return new AnimationNodeDebugSnapshot(nameof(HumanoidLeftHandIkNode), scriptPlayable.IsValid(), currentWeight, 1);
        }

        protected override void OnInitialize(AnimationGraphContext context)
        {
            inputNode.Initialize(context);
            scriptPlayable = AnimationScriptPlayable.Create(context.Graph, new HumanoidLeftHandIkJob(), 1);
            context.Graph.Connect(inputNode.Evaluate(context).Playable, 0, scriptPlayable, 0);
            scriptPlayable.SetInputWeight(0, 1f);
        }

        protected override void OnDestroy()
        {
            binding = null;
            inputNode.Destroy();
        }
    }
}
