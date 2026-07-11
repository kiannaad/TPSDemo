using System;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CGame.Animation
{
    public sealed class PriorityNode : AnimationNodeBase
    {
        private readonly ActionNode[] actions;
        private AnimationMixerPlayable mixerPlayable;
        private ActionNode activeAction;

        public PriorityNode(params ActionNode[] actions)
        {
            this.actions = actions ?? Array.Empty<ActionNode>();
        }

        public ActionNode ActiveAction => activeAction;
        public AnimationMixerPlayable MixerPlayable => mixerPlayable;

        public override void Update(AnimationGraphContext context, float deltaTime)
        {
            for (int i = 0; i < actions.Length; i++)
            {
                actions[i]?.Update(context, deltaTime);
            }

            ActionNode selected = SelectHighestPriorityAction();
            if (activeAction != null && selected != activeAction && selected != null)
            {
                context.RecordDebugEvent(nameof(PriorityNode), $"{activeAction.Priority}->Interrupted", activeAction.Weight);
                activeAction.Interrupt();
            }

            if (selected != activeAction && selected != null)
            {
                context.RecordDebugEvent(nameof(PriorityNode), $"Priority:{selected.Priority}", 1f);
            }

            activeAction = selected;
            for (int i = 0; i < actions.Length; i++)
            {
                ActionNode action = actions[i];
                float weight = action != null && action == activeAction ? 1f : 0f;
                if (action != null)
                {
                    action.Weight = weight;
                }

                mixerPlayable.SetInputWeight(i, weight);
            }

            context.DebugActiveAction = activeAction != null ? $"Priority:{activeAction.Priority}" : string.Empty;
            context.DebugActiveActionWeight = activeAction != null ? activeAction.Weight : 0f;
        }

        public override AnimationPoseHandle Evaluate(AnimationGraphContext context)
        {
            return new AnimationPoseHandle(mixerPlayable, 1f, context.EvaluateFrameId, nameof(PriorityNode));
        }

        public override AnimationNodeDebugSnapshot GetDebugSnapshot()
        {
            return new AnimationNodeDebugSnapshot(nameof(PriorityNode), mixerPlayable.IsValid(), activeAction != null ? 1f : 0f, actions.Length);
        }

        protected override void OnInitialize(AnimationGraphContext context)
        {
            mixerPlayable = AnimationMixerPlayable.Create(context.Graph, actions.Length);
            for (int i = 0; i < actions.Length; i++)
            {
                ActionNode action = actions[i];
                if (action == null)
                {
                    continue;
                }

                action.Initialize(context);
                context.Graph.Connect(action.Evaluate(context).Playable, 0, mixerPlayable, i);
                mixerPlayable.SetInputWeight(i, 0f);
            }
        }

        protected override void OnDestroy()
        {
            for (int i = 0; i < actions.Length; i++)
            {
                actions[i]?.Destroy();
            }
        }

        private ActionNode SelectHighestPriorityAction()
        {
            ActionNode selected = null;
            for (int i = 0; i < actions.Length; i++)
            {
                ActionNode action = actions[i];
                if (action != null && action.IsActive && (selected == null || action.Priority > selected.Priority))
                {
                    selected = action;
                }
            }

            return selected;
        }
    }
}
