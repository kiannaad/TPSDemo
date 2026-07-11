using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CGame.Animation
{
    public sealed class ClipNode : AnimationNodeBase
    {
        private readonly AnimationClip clip;
        private AnimationClipPlayable clipPlayable;

        public ClipNode(AnimationClip clip, float speed = 1f)
        {
            this.clip = clip;
            Speed = speed;
        }

        public AnimationClip Clip => clip;
        public float Speed { get; set; }
        public bool Loop { get; set; } = true;
        public AnimationClipPlayable ClipPlayable => clipPlayable;

        public override AnimationPoseHandle Evaluate(AnimationGraphContext context)
        {
            return new AnimationPoseHandle(clipPlayable, 1f, context.EvaluateFrameId, nameof(ClipNode));
        }

        public override AnimationNodeDebugSnapshot GetDebugSnapshot()
        {
            return new AnimationNodeDebugSnapshot(nameof(ClipNode), clipPlayable.IsValid(), 1f, 0);
        }

        protected override void OnInitialize(AnimationGraphContext context)
        {
            if (clip == null)
            {
                throw new InvalidOperationException("ClipNode requires an AnimationClip.");
            }

            clipPlayable = AnimationClipPlayable.Create(context.Graph, clip);
            clipPlayable.SetSpeed(Speed);
            clipPlayable.SetDuration(clip.length);
            clipPlayable.SetTime(0d);
            clipPlayable.SetApplyFootIK(false);
        }
    }
}
