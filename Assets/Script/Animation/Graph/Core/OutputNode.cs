using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CGame.Animation
{
    public sealed class OutputNode : IDisposable
    {
        private readonly IAnimationPlayableNode sourceNode;
        private readonly string graphName;
        private PlayableGraph graph;
        private AnimationPlayableOutput output;
        private AnimationGraphContext context;
        private bool initialized;

        public OutputNode(IAnimationPlayableNode sourceNode, string graphName = "CGameAnimationGraph")
        {
            this.sourceNode = sourceNode;
            this.graphName = string.IsNullOrWhiteSpace(graphName)
                ? "CGameAnimationGraph"
                : graphName;
        }

        public PlayableGraph Graph => graph;
        public AnimationGraphContext Context => context;
        public bool IsInitialized => initialized && graph.IsValid();
        public Playable SourcePlayable => output.IsOutputValid() ? output.GetSourcePlayable() : Playable.Null;

        public void Initialize(Animator animator)
        {
            if (initialized)
            {
                return;
            }

            if (animator == null)
            {
                throw new ArgumentNullException(nameof(animator));
            }

            if (sourceNode == null)
            {
                throw new InvalidOperationException("OutputNode requires a source node.");
            }

            graph = PlayableGraph.Create(graphName);
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            output = AnimationPlayableOutput.Create(graph, "Animation", animator);
            context = new AnimationGraphContext(animator, graph);

            sourceNode.Initialize(context);
            context.BeginEvaluateFrame();
            Playable sourcePlayable = sourceNode.Evaluate(context);
            if (!sourcePlayable.IsValid())
            {
                throw new InvalidOperationException("OutputNode source node produced an invalid playable.");
            }

            output.SetSourcePlayable(sourcePlayable);
            graph.Play();
            initialized = true;
        }

        public void Update(float deltaTime)
        {
            if (!IsInitialized)
            {
                return;
            }

            context.DeltaTime = deltaTime;
            context.ElapsedTime += Mathf.Max(0f, deltaTime);
            context.ResetRootMotionDelta();
            sourceNode.Update(context, deltaTime);
            context.BeginEvaluateFrame();
            Playable sourcePlayable = sourceNode.Evaluate(context);
            if (sourcePlayable.IsValid() && !sourcePlayable.Equals(output.GetSourcePlayable()))
            {
                output.SetSourcePlayable(sourcePlayable);
            }
        }

        public AnimationNodeDebugSnapshot GetDebugSnapshot()
        {
            return sourceNode != null
                ? sourceNode.GetDebugSnapshot()
                : AnimationNodeDebugSnapshot.Invalid("Output");
        }

        public AnimationGraphDebugSnapshot GetGraphDebugSnapshot()
        {
            if (context == null)
            {
                return new AnimationGraphDebugSnapshot(
                    AnimationNodeDebugSnapshot.Invalid("Output"), string.Empty, 1f, string.Empty, 0f, null);
            }

            var events = new AnimationDebugEvent[context.DebugEvents.Count];
            for (int i = 0; i < events.Length; i++)
            {
                events[i] = context.DebugEvents[i];
            }

            return new AnimationGraphDebugSnapshot(
                GetDebugSnapshot(),
                context.DebugLocomotionState,
                context.DebugFadeProgress,
                context.DebugActiveAction,
                context.DebugActiveActionWeight,
                events);
        }

        public void Dispose()
        {
            Destroy();
        }

        public void Destroy()
        {
            if (!initialized)
            {
                return;
            }

            sourceNode.Destroy();
            if (graph.IsValid())
            {
                graph.Destroy();
            }

            context = null;
            initialized = false;
        }
    }
}
