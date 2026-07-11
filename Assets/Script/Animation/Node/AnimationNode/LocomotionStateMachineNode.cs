using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CGame.Animation
{
    public sealed class LocomotionStateMachineNode : AnimationNodeBase
    {
        private readonly IdleState idleState;
        private readonly MoveState moveState;
        private readonly StopState stopState;
        private readonly float moveThreshold;
        private readonly float stopDuration;
        private readonly float fadeDuration;
        private AnimationMixerPlayable mixerPlayable;
        private LocomotionStateBase currentState;
        private LocomotionStateBase fadingFromState;
        private float fadeElapsed;

        public LocomotionStateMachineNode(
            IdleState idleState,
            MoveState moveState,
            StopState stopState,
            float moveThreshold = 0.1f,
            float stopDuration = 0.2f,
            float fadeDuration = 0.15f)
        {
            this.idleState = idleState ?? throw new ArgumentNullException(nameof(idleState));
            this.moveState = moveState ?? throw new ArgumentNullException(nameof(moveState));
            this.stopState = stopState ?? throw new ArgumentNullException(nameof(stopState));
            this.moveThreshold = Mathf.Max(0f, moveThreshold);
            this.stopDuration = Mathf.Max(0f, stopDuration);
            this.fadeDuration = Mathf.Max(0f, fadeDuration);
        }

        public event Action<LocomotionState, LocomotionStatePhase> StatePhaseChanged;

        public LocomotionState CurrentState => currentState != null ? currentState.State : LocomotionState.Idle;
        public LocomotionState? FadingFromState => fadingFromState?.State;
        public float TransitionAlpha => fadingFromState == null ? 1f : GetTransitionAlpha();
        public AnimationMixerPlayable MixerPlayable => mixerPlayable;

        public override void Update(AnimationGraphContext context, float deltaTime)
        {
            currentState.Update(context, deltaTime);

            bool transitioned = TryTransition(context);
            if (!transitioned && fadingFromState != null)
            {
                fadeElapsed += Mathf.Max(0f, deltaTime);
                if (GetTransitionAlpha() >= 1f)
                {
                    fadingFromState = null;
                }
            }

            UpdateDebugState(context);
        }

        public override AnimationPoseHandle Evaluate(AnimationGraphContext context)
        {
            currentState.Evaluate(context);
            ApplyWeights();
            return new AnimationPoseHandle(mixerPlayable, 1f, context.EvaluateFrameId, nameof(LocomotionStateMachineNode));
        }

        public override AnimationNodeDebugSnapshot GetDebugSnapshot()
        {
            return new AnimationNodeDebugSnapshot(nameof(LocomotionStateMachineNode), mixerPlayable.IsValid(), 1f, 3);
        }

        protected override void OnInitialize(AnimationGraphContext context)
        {
            mixerPlayable = AnimationMixerPlayable.Create(context.Graph, 3);
            InitializeState(context, idleState, 0);
            InitializeState(context, moveState, 1);
            InitializeState(context, stopState, 2);

            currentState = idleState;
            currentState.Enter(context);
            ApplyWeights();
            UpdateDebugState(context);
        }

        protected override void OnDestroy()
        {
            idleState.Node.Destroy();
            moveState.Node.Destroy();
            stopState.Node.Destroy();
            currentState = null;
            fadingFromState = null;
        }

        private void InitializeState(AnimationGraphContext context, LocomotionStateBase state, int inputIndex)
        {
            state.SetPhaseReporter(ReportStatePhase);
            state.Node.Initialize(context);
            Playable playable = state.Node.Evaluate(context);
            if (!playable.IsValid())
            {
                throw new InvalidOperationException($"{state.State} state produced an invalid playable.");
            }

            context.Graph.Connect(playable, 0, mixerPlayable, inputIndex);
        }

        private bool TryTransition(AnimationGraphContext context)
        {
            bool isMoving = context.MoveSpeed > moveThreshold;
            if (currentState == idleState && isMoving)
            {
                BeginTransition(context, moveState);
                return true;
            }

            if (currentState == moveState && !isMoving)
            {
                BeginTransition(context, stopState);
                return true;
            }

            if (currentState == stopState && isMoving)
            {
                BeginTransition(context, moveState);
                return true;
            }

            if (currentState == stopState && stopState.ElapsedTime >= stopDuration)
            {
                BeginTransition(context, idleState);
                return true;
            }

            return false;
        }

        private void BeginTransition(AnimationGraphContext context, LocomotionStateBase nextState)
        {
            if (nextState == fadingFromState)
            {
                LocomotionStateBase previousTarget = currentState;
                previousTarget.Exit(context);
                currentState = nextState;
                fadingFromState = previousTarget;
                fadeElapsed = Mathf.Max(0f, fadeDuration - fadeElapsed);
                currentState.Enter(context);
                context.RecordDebugEvent(nameof(LocomotionStateMachineNode), $"{previousTarget.State}->{currentState.State}");
                return;
            }

            LocomotionStateBase previousState = currentState;
            previousState.Exit(context);
            fadingFromState = previousState;
            currentState = nextState;
            fadeElapsed = 0f;
            currentState.Enter(context);
            context.RecordDebugEvent(nameof(LocomotionStateMachineNode), $"{previousState.State}->{currentState.State}");
        }

        private void ApplyWeights()
        {
            if (!mixerPlayable.IsValid())
            {
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                mixerPlayable.SetInputWeight(i, 0f);
            }

            if (fadingFromState == null)
            {
                mixerPlayable.SetInputWeight(GetInputIndex(currentState), 1f);
                return;
            }

            float alpha = GetTransitionAlpha();
            mixerPlayable.SetInputWeight(GetInputIndex(fadingFromState), 1f - alpha);
            mixerPlayable.SetInputWeight(GetInputIndex(currentState), alpha);
        }

        private float GetTransitionAlpha()
        {
            return fadeDuration <= 0f ? 1f : Mathf.Clamp01(fadeElapsed / fadeDuration);
        }

        private static int GetInputIndex(LocomotionStateBase state)
        {
            return (int)state.State;
        }

        private void ReportStatePhase(LocomotionState state, LocomotionStatePhase phase)
        {
            StatePhaseChanged?.Invoke(state, phase);
        }

        private void UpdateDebugState(AnimationGraphContext context)
        {
            context.DebugLocomotionState = CurrentState.ToString();
            context.DebugFadeProgress = TransitionAlpha;
        }
    }
}
