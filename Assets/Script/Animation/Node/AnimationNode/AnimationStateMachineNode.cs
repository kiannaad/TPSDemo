using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CGame.Animation
{
    public sealed class AnimationStateMachineNode : AnimationNodeBase
    {
        private readonly AnimationState[] states;
        private readonly AnimationStateTransition[] transitions;
        private readonly Dictionary<string, int> stateIndices = new Dictionary<string, int>();
        private readonly float[] currentWeights;
        private readonly float[] transitionStartWeights;
        private AnimationMixerPlayable mixerPlayable;
        private AnimationState currentState;
        private AnimationStateTransition activeTransition;
        private float transitionElapsed;

        public AnimationStateMachineNode(
            AnimationState[] states,
            AnimationStateTransition[] transitions,
            string initialState)
        {
            this.states = states ?? Array.Empty<AnimationState>();
            this.transitions = transitions ?? Array.Empty<AnimationStateTransition>();
            InitialState = initialState ?? string.Empty;
            currentWeights = new float[this.states.Length];
            transitionStartWeights = new float[this.states.Length];
        }

        public event Action<string, LocomotionStatePhase> StatePhaseChanged;

        public string InitialState { get; }
        public string CurrentState => currentState?.Name ?? string.Empty;
        public AnimationStateTransition ActiveTransition => activeTransition;
        public float TransitionAlpha => activeTransition == null ? 1f : GetTransitionAlpha();
        public AnimationMixerPlayable MixerPlayable => mixerPlayable;

        public override void Update(AnimationGraphContext context, float deltaTime)
        {
            currentState.Update(context, deltaTime);
            AnimationStateTransition selected = SelectTransition(context);
            if (selected != null)
            {
                BeginTransition(context, selected);
            }
            else if (activeTransition != null)
            {
                transitionElapsed += Mathf.Max(0f, deltaTime);
                ApplyTransitionWeights();
                if (GetTransitionAlpha() >= 1f)
                {
                    activeTransition = null;
                }
            }

            UpdateDebugState(context);
        }

        public override AnimationPoseHandle Evaluate(AnimationGraphContext context)
        {
            currentState.Evaluate(context);
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i] != currentState && currentWeights[i] > 0f)
                {
                    states[i].Node.Evaluate(context);
                }
            }

            return new AnimationPoseHandle(mixerPlayable, 1f, context.EvaluateFrameId, nameof(AnimationStateMachineNode));
        }

        public override AnimationNodeDebugSnapshot GetDebugSnapshot()
        {
            return new AnimationNodeDebugSnapshot(nameof(AnimationStateMachineNode), mixerPlayable.IsValid(), 1f, states.Length);
        }

        protected override void OnInitialize(AnimationGraphContext context)
        {
            if (states.Length == 0)
            {
                throw new InvalidOperationException("AnimationStateMachineNode requires at least one state.");
            }

            mixerPlayable = AnimationMixerPlayable.Create(context.Graph, states.Length);
            for (int i = 0; i < states.Length; i++)
            {
                AnimationState state = states[i] ?? throw new InvalidOperationException("State entries cannot be null.");
                if (stateIndices.ContainsKey(state.Name))
                {
                    throw new InvalidOperationException($"Duplicate animation state '{state.Name}'.");
                }

                stateIndices.Add(state.Name, i);
                state.SetPhaseReporter(ReportStatePhase);
                state.Node.Initialize(context);
                AnimationPoseHandle pose = state.Node.Evaluate(context);
                if (!pose.Playable.IsValid())
                {
                    throw new InvalidOperationException($"Animation state '{state.Name}' produced an invalid playable.");
                }

                context.Graph.Connect(pose.Playable, 0, mixerPlayable, i);
                mixerPlayable.SetInputWeight(i, 0f);
            }

            if (!stateIndices.TryGetValue(InitialState, out int initialIndex))
            {
                throw new InvalidOperationException($"Initial animation state '{InitialState}' does not exist.");
            }

            ValidateTransitions();
            currentState = states[initialIndex];
            currentWeights[initialIndex] = 1f;
            mixerPlayable.SetInputWeight(initialIndex, 1f);
            currentState.Enter(context);
            UpdateDebugState(context);
        }

        protected override void OnDestroy()
        {
            for (int i = 0; i < states.Length; i++)
            {
                states[i]?.Node.Destroy();
            }

            currentState = null;
            activeTransition = null;
            stateIndices.Clear();
            Array.Clear(currentWeights, 0, currentWeights.Length);
            Array.Clear(transitionStartWeights, 0, transitionStartWeights.Length);
        }

        private AnimationStateTransition SelectTransition(AnimationGraphContext context)
        {
            AnimationStateTransition selected = null;
            for (int i = 0; i < transitions.Length; i++)
            {
                AnimationStateTransition transition = transitions[i];
                if (!transition.IsAnyState && transition.FromState != CurrentState)
                {
                    continue;
                }

                if (transition.ToState == CurrentState || !transition.Condition(context))
                {
                    continue;
                }

                if (activeTransition != null
                    && (!activeTransition.CanBeInterrupted || transition.Priority < activeTransition.Priority))
                {
                    continue;
                }

                if (selected == null || transition.Priority > selected.Priority)
                {
                    selected = transition;
                }
            }

            return selected;
        }

        private void BeginTransition(AnimationGraphContext context, AnimationStateTransition transition)
        {
            for (int i = 0; i < currentWeights.Length; i++)
            {
                transitionStartWeights[i] = currentWeights[i];
            }

            string previousState = CurrentState;
            currentState.Exit(context);
            currentState = states[stateIndices[transition.ToState]];
            currentState.Enter(context);
            activeTransition = transition;
            transitionElapsed = 0f;
            ApplyTransitionWeights();
            context.RecordDebugEvent(nameof(AnimationStateMachineNode), $"{previousState}->{CurrentState}", transition.Priority);
        }

        private void ApplyTransitionWeights()
        {
            float alpha = GetTransitionAlpha();
            int targetIndex = stateIndices[CurrentState];
            for (int i = 0; i < currentWeights.Length; i++)
            {
                float target = i == targetIndex ? 1f : 0f;
                currentWeights[i] = Mathf.Lerp(transitionStartWeights[i], target, alpha);
                mixerPlayable.SetInputWeight(i, currentWeights[i]);
            }
        }

        private float GetTransitionAlpha()
        {
            return activeTransition == null || activeTransition.BlendDuration <= 0f
                ? 1f
                : Mathf.Clamp01(transitionElapsed / activeTransition.BlendDuration);
        }

        private void ValidateTransitions()
        {
            for (int i = 0; i < transitions.Length; i++)
            {
                AnimationStateTransition transition = transitions[i];
                if (transition == null)
                {
                    throw new InvalidOperationException("Transition entries cannot be null.");
                }

                if ((!transition.IsAnyState && !stateIndices.ContainsKey(transition.FromState))
                    || !stateIndices.ContainsKey(transition.ToState))
                {
                    throw new InvalidOperationException($"Transition '{transition.FromState}->{transition.ToState}' references an unknown state.");
                }
            }
        }

        private void UpdateDebugState(AnimationGraphContext context)
        {
            context.DebugLocomotionState = CurrentState;
            context.DebugFadeProgress = TransitionAlpha;
        }

        private void ReportStatePhase(string state, LocomotionStatePhase phase)
        {
            StatePhaseChanged?.Invoke(state, phase);
        }
    }
}
