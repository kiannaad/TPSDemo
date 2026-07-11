using System;
using UnityEngine.Playables;

namespace CGame.Animation
{
    public sealed class AnimationState
    {
        private Action<string, LocomotionStatePhase> phaseReporter;

        public AnimationState(string name, IAnimationPlayableNode node)
        {
            Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("State name is required.", nameof(name)) : name;
            Node = node ?? throw new ArgumentNullException(nameof(node));
        }

        public string Name { get; }
        public IAnimationPlayableNode Node { get; }
        public bool IsActive { get; private set; }

        public void Enter(AnimationGraphContext context)
        {
            IsActive = true;
            AnimationPoseHandle pose = Node.Evaluate(context);
            if (pose.Playable.IsValid())
            {
                pose.Playable.SetTime(0d);
            }

            phaseReporter?.Invoke(Name, LocomotionStatePhase.Enter);
        }

        public void Update(AnimationGraphContext context, float deltaTime)
        {
            phaseReporter?.Invoke(Name, LocomotionStatePhase.Update);
            Node.Update(context, deltaTime);
        }

        public void Exit(AnimationGraphContext context)
        {
            IsActive = false;
            phaseReporter?.Invoke(Name, LocomotionStatePhase.Exit);
        }

        public AnimationPoseHandle Evaluate(AnimationGraphContext context)
        {
            phaseReporter?.Invoke(Name, LocomotionStatePhase.Evaluate);
            return Node.Evaluate(context);
        }

        internal void SetPhaseReporter(Action<string, LocomotionStatePhase> reporter)
        {
            phaseReporter = reporter;
        }
    }
}
