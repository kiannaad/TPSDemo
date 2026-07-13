using CGame.Animation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;

namespace CGame.Tests
{
    public class AnimationNodeTransitionDebugTests
    {
        [Test]
        public void GraphDebugSnapshot_ExposesStateFadeActionAndEventsWithoutChangingWeights()
        {
            GameObject gameObject = new GameObject("AnimationDebugSnapshotTest");
            OutputNode output = null;
            AnimationClipAsset actionAsset = null;
            try
            {
                Animator animator = gameObject.AddComponent<Animator>();
                var locomotion = new LocomotionStateMachineNode(
                    new IdleState(new ClipNode(CreateClip())),
                    new MoveState(new ClipNode(CreateClip())),
                    new StopState(new ClipNode(CreateClip())),
                    fadeDuration: 0.1f);
                actionAsset = ScriptableObject.CreateInstance<AnimationClipAsset>();
                actionAsset.TryInitialize(CreateClip());
                var action = new ActionNode(actionAsset, 20);
                var priority = new PriorityNode(action);
                var root = new LayerBoneNode(locomotion, priority, new AvatarMask(), context => context.OverlayWeight);
                output = new OutputNode(root, "AnimationDebugSnapshotTest");
                output.Initialize(animator);
                output.Context.MoveSpeed = 1f;
                output.Context.OverlayWeight = 1f;
                action.Request();
                output.Update(0f);
                output.Update(0.05f);

                float idleWeight = locomotion.MixerPlayable.GetInputWeight(0);
                float moveWeight = locomotion.MixerPlayable.GetInputWeight(1);
                float actionWeight = priority.MixerPlayable.GetInputWeight(0);
                AnimationGraphDebugSnapshot snapshot = output.GetGraphDebugSnapshot();

                Assert.AreEqual(nameof(LayerBoneNode), snapshot.RootNode.NodeName);
                Assert.AreEqual("Move", snapshot.LocomotionState);
                Assert.AreEqual(0.5f, snapshot.FadeProgress, 0.001f);
                Assert.AreEqual("Priority:20", snapshot.ActiveAction);
                Assert.AreEqual(1f, snapshot.ActiveActionWeight);
                Assert.GreaterOrEqual(snapshot.Events.Count, 2);
                Assert.AreEqual(idleWeight, locomotion.MixerPlayable.GetInputWeight(0));
                Assert.AreEqual(moveWeight, locomotion.MixerPlayable.GetInputWeight(1));
                Assert.AreEqual(actionWeight, priority.MixerPlayable.GetInputWeight(0));
            }
            finally
            {
                output?.Destroy();
                if (actionAsset != null) Object.DestroyImmediate(actionAsset);
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void GraphDebugSnapshot_CopiesEventHistoryAndContextKeepsLatestThirtyTwo()
        {
            GameObject gameObject = new GameObject("AnimationDebugEventHistoryTest");
            OutputNode output = null;
            try
            {
                Animator animator = gameObject.AddComponent<Animator>();
                output = new OutputNode(new ClipNode(CreateClip()), "AnimationDebugEventHistoryTest");
                output.Initialize(animator);
                for (int i = 0; i < 40; i++)
                {
                    output.Context.RecordDebugEvent("Test", $"Event:{i}", i);
                }

                AnimationGraphDebugSnapshot snapshot = output.GetGraphDebugSnapshot();
                output.Context.RecordDebugEvent("Test", "Later");

                Assert.AreEqual(32, snapshot.Events.Count);
                Assert.AreEqual("Event:8", snapshot.Events[0].EventName);
                Assert.AreEqual("Event:39", snapshot.Events[31].EventName);
            }
            finally
            {
                output?.Destroy();
                Object.DestroyImmediate(gameObject);
            }
        }

        private static AnimationClip CreateClip()
        {
            var clip = new AnimationClip { frameRate = 30f };
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 1f));
            return clip;
        }
    }
}
