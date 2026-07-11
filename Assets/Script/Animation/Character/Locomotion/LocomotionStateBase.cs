using System;
using UnityEngine.Playables;

namespace CGame.Animation
{
    public abstract class LocomotionStateBase
    {
        private readonly IAnimationPlayableNode node;
        private Action<LocomotionState, LocomotionStatePhase> phaseReporter;

        protected LocomotionStateBase(LocomotionState state, IAnimationPlayableNode node)
        {
            State = state;
            this.node = node ?? throw new ArgumentNullException(nameof(node));
        }

        public LocomotionState State { get; }
        public IAnimationPlayableNode Node => node;
        public bool IsActive { get; private set; }

        public virtual void Enter(AnimationGraphContext context)
        {
            IsActive = true;
            Playable playable = node.Evaluate(context);
            if (playable.IsValid())
            {
                playable.SetTime(0d);
            }

            Report(LocomotionStatePhase.Enter);
        }

        public virtual void Update(AnimationGraphContext context, float deltaTime)
        {
            Report(LocomotionStatePhase.Update);
            node.Update(context, deltaTime);
        }

        public virtual void Exit(AnimationGraphContext context)
        {
            IsActive = false;
            Report(LocomotionStatePhase.Exit);
        }

        public virtual AnimationPoseHandle Evaluate(AnimationGraphContext context)
        {
            Report(LocomotionStatePhase.Evaluate);
            return node.Evaluate(context);
        }

        internal void SetPhaseReporter(Action<LocomotionState, LocomotionStatePhase> reporter)
        {
            phaseReporter = reporter;
        }

        private void Report(LocomotionStatePhase phase)
        {
            phaseReporter?.Invoke(State, phase);
        }
    }
}
