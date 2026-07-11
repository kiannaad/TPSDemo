using System;

namespace CGame.Animation
{
    public sealed class AnimationStateTransition
    {
        public AnimationStateTransition(
            string fromState,
            string toState,
            Func<AnimationGraphContext, bool> condition,
            int priority = 0,
            float blendDuration = 0.15f,
            bool canBeInterrupted = true)
        {
            FromState = fromState ?? string.Empty;
            ToState = string.IsNullOrWhiteSpace(toState)
                ? throw new ArgumentException("Target state is required.", nameof(toState))
                : toState;
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            Priority = priority;
            BlendDuration = Math.Max(0f, blendDuration);
            CanBeInterrupted = canBeInterrupted;
        }

        public string FromState { get; }
        public string ToState { get; }
        public Func<AnimationGraphContext, bool> Condition { get; }
        public int Priority { get; }
        public float BlendDuration { get; }
        public bool CanBeInterrupted { get; }
        public bool IsAnyState => string.IsNullOrEmpty(FromState);
    }
}
