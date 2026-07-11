using System;
using System.Collections.Generic;

namespace CGame.Animation
{
    public sealed class AnimationGraphDebugSnapshot
    {
        public AnimationGraphDebugSnapshot(
            AnimationNodeDebugSnapshot rootNode,
            string locomotionState,
            float fadeProgress,
            string activeAction,
            float activeActionWeight,
            AnimationDebugEvent[] events)
        {
            RootNode = rootNode;
            LocomotionState = locomotionState ?? string.Empty;
            FadeProgress = fadeProgress;
            ActiveAction = activeAction ?? string.Empty;
            ActiveActionWeight = activeActionWeight;
            Events = Array.AsReadOnly(events ?? Array.Empty<AnimationDebugEvent>());
        }

        public AnimationNodeDebugSnapshot RootNode { get; }
        public string LocomotionState { get; }
        public float FadeProgress { get; }
        public string ActiveAction { get; }
        public float ActiveActionWeight { get; }
        public IReadOnlyList<AnimationDebugEvent> Events { get; }
    }
}
