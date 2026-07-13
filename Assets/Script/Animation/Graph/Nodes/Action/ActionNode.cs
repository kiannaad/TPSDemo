using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CGame.Animation
{
    public sealed class ActionNode : AnimationNodeBase
    {
        private readonly AnimationClipAsset clipAsset;
        private AnimationClipPlayable clipPlayable;
        private AnimationNotifyRuntime notifyRuntime;
        private bool requested;

        public ActionNode(AnimationClipAsset clipAsset, int priority)
        {
            this.clipAsset = clipAsset ?? throw new ArgumentNullException(nameof(clipAsset));
            Priority = priority;
        }

        public int Priority { get; }
        public bool IsActive { get; private set; }
        public float Weight { get; internal set; }
        public AnimationClipPlayable ClipPlayable => clipPlayable;

        public void Request()
        {
            requested = true;
        }

        public void Interrupt()
        {
            if (!IsActive)
            {
                return;
            }

            notifyRuntime?.EndAll(AnimationNotifyEndReason.Interrupted);
            IsActive = false;
            Weight = 0f;
        }

        public override void Update(AnimationGraphContext context, float deltaTime)
        {
            if (requested)
            {
                requested = false;
                IsActive = true;
                clipPlayable.SetTime(0d);
                clipPlayable.SetDone(false);
                context.RecordDebugEvent(nameof(ActionNode), $"Started:{Priority}", 1f);
            }

            if (!IsActive)
            {
                return;
            }

            float currentTime = Mathf.Max(0f, (float)clipPlayable.GetTime());
            notifyRuntime.Tick(currentTime, deltaTime, Weight);
            if (currentTime >= clipAsset.AnimationClip.length)
            {
                notifyRuntime.EndAll(AnimationNotifyEndReason.NaturalEnd);
                IsActive = false;
                Weight = 0f;
                context.RecordDebugEvent(nameof(ActionNode), $"Ended:{Priority}", currentTime);
            }
        }

        public override AnimationPoseHandle Evaluate(AnimationGraphContext context)
        {
            return new AnimationPoseHandle(clipPlayable, Weight, context.EvaluateFrameId, $"Action:{Priority}");
        }

        public override AnimationNodeDebugSnapshot GetDebugSnapshot()
        {
            return new AnimationNodeDebugSnapshot(nameof(ActionNode), clipPlayable.IsValid(), Weight, 0);
        }

        protected override void OnInitialize(AnimationGraphContext context)
        {
            if (!clipAsset.IsValid)
            {
                throw new InvalidOperationException("ActionNode requires a valid AnimationClipAsset.");
            }

            clipPlayable = AnimationClipPlayable.Create(context.Graph, clipAsset.AnimationClip);
            clipPlayable.SetDuration(clipAsset.AnimationClip.length);
            clipPlayable.SetSpeed(clipAsset.Speed);
            notifyRuntime = new AnimationNotifyRuntime(context.Animator, clipAsset);
        }

        protected override void OnDestroy()
        {
            notifyRuntime?.EndAll(AnimationNotifyEndReason.Interrupted);
        }
    }
}
