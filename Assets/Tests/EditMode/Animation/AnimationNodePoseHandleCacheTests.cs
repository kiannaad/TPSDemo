using System;
using CGame.Animation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace CGame.Tests
{
    public class AnimationNodePoseHandleCacheTests
    {
        [Test]
        public void AnimationPoseHandle_ExposesPlayableWeightFrameAndSource()
        {
            using (var fixture = new GraphFixture())
            {
                AnimationClipPlayable playable = AnimationClipPlayable.Create(fixture.Graph, CreateClip());
                var handle = new AnimationPoseHandle(playable, 0.75f, 42, "TestSource");

                Assert.IsTrue(handle.IsValid);
                Assert.AreEqual(0.75f, handle.Weight);
                Assert.AreEqual(42, handle.EvaluateFrameId);
                Assert.AreEqual("TestSource", handle.Source);
                Assert.AreEqual((Playable)playable, handle.Playable);
            }
        }

        [Test]
        public void CachedPoseNode_CachesEvaluatePerFrameButAlwaysForwardsUpdateAndRootDelta()
        {
            using (var fixture = new GraphFixture())
            {
                var source = new CountingPoseNode();
                var cached = new CachedPoseNode(source);
                cached.Initialize(fixture.Context);
                fixture.Context.BeginEvaluateFrame();

                AnimationPoseHandle first = cached.Evaluate(fixture.Context);
                AnimationPoseHandle second = cached.Evaluate(fixture.Context);
                cached.Update(fixture.Context, 0.016f);
                cached.Update(fixture.Context, 0.016f);

                Assert.AreEqual(1, source.EvaluateCount);
                Assert.AreEqual(first.Playable, second.Playable);
                Assert.AreEqual(2, source.UpdateCount);
                Assert.AreEqual(0.2f, fixture.Context.RootMotionDelta.PositionDelta.x, 0.001f);

                fixture.Context.BeginEvaluateFrame();
                AnimationPoseHandle nextFrame = cached.Evaluate(fixture.Context);
                Assert.AreEqual(2, source.EvaluateCount);
                Assert.AreEqual(fixture.Context.EvaluateFrameId, nextFrame.EvaluateFrameId);
            }
        }

        [Test]
        public void ArtIdlePose_CachedBranchOutputsToRobotAnimator()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Animation/FemaleLocomotionSet/Prefabs/Robot Kyle.prefab");
            AnimationClipAsset idle = AssetDatabase.LoadAssetAtPath<AnimationClipAsset>(
                "Assets/Art/Animation/LocomotionAsset/InPlace/A_INP_IdleClipAsset.asset");
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(idle);
            Assert.IsTrue(idle.IsValid);
            GameObject robot = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            OutputNode output = null;
            try
            {
                Animator animator = robot.GetComponentInChildren<Animator>();
                var cached = new CachedPoseNode(new ClipNode(idle.AnimationClip));
                output = new OutputNode(cached, "ArtCachedPoseTest");
                output.Initialize(animator);
                long initialFrame = output.Context.EvaluateFrameId;
                output.Update(0.016f);
                AnimationPoseHandle cachedPose = cached.CachedPose;

                Assert.IsTrue(output.Graph.IsValid());
                Assert.IsTrue(cachedPose.IsValid);
                Assert.Greater(output.Context.EvaluateFrameId, initialFrame);
                Assert.AreEqual(output.Context.EvaluateFrameId, cachedPose.EvaluateFrameId);
            }
            finally
            {
                output?.Destroy();
                Object.DestroyImmediate(robot);
            }
        }

        [Test]
        public void CachedPoseNode_DoesNotSuppressActionNotifyInterruptionCleanup()
        {
            using (var fixture = new GraphFixture())
            {
                var notify = new RecordingDurationNotify();
                AnimationClipAsset asset = ScriptableObject.CreateInstance<AnimationClipAsset>();
                asset.TryInitialize(CreateClip());
                AnimationNotifyEvent notifyEvent = asset.AddNotifyTrack().AddEvent(notify, 1, 20);
                notifyEvent.MinTriggerWeight = 0f;
                var action = new ActionNode(asset, 10);
                var cached = new CachedPoseNode(action);
                cached.Initialize(fixture.Context);
                fixture.Context.BeginEvaluateFrame();
                action.Request();
                cached.Update(fixture.Context, 0f);
                action.ClipPlayable.SetTime(0.1d);
                cached.Update(fixture.Context, 0.1f);
                cached.Evaluate(fixture.Context);

                action.Interrupt();

                Assert.AreEqual(1, notify.BeginCount);
                Assert.AreEqual(1, notify.EndCount);
                Assert.AreEqual(AnimationNotifyEndReason.Interrupted, notify.EndReason);
                Object.DestroyImmediate(asset);
            }
        }

        private static AnimationClip CreateClip()
        {
            var clip = new AnimationClip { frameRate = 30f };
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 1f));
            return clip;
        }

        private sealed class CountingPoseNode : AnimationNodeBase
        {
            private AnimationMixerPlayable mixer;

            public int UpdateCount { get; private set; }
            public int EvaluateCount { get; private set; }

            public override void Update(AnimationGraphContext context, float deltaTime)
            {
                UpdateCount++;
                context.AccumulateRootMotionDelta(
                    new AnimationRootMotionDelta(new Vector3(0.1f, 0f, 0f), Quaternion.identity, 1f));
            }

            public override AnimationPoseHandle Evaluate(AnimationGraphContext context)
            {
                EvaluateCount++;
                return new AnimationPoseHandle(mixer, 1f, context.EvaluateFrameId, nameof(CountingPoseNode));
            }

            public override AnimationNodeDebugSnapshot GetDebugSnapshot()
            {
                return new AnimationNodeDebugSnapshot(nameof(CountingPoseNode), mixer.IsValid(), 1f, 0);
            }

            protected override void OnInitialize(AnimationGraphContext context)
            {
                mixer = AnimationMixerPlayable.Create(context.Graph, 0);
            }
        }

        [Serializable]
        private sealed class RecordingDurationNotify : AnimationDurationNotify
        {
            public int BeginCount { get; private set; }
            public int EndCount { get; private set; }
            public AnimationNotifyEndReason EndReason { get; private set; }

            public override void OnBegin(AnimationEventContext context) => BeginCount++;

            public override void OnEnd(AnimationEventContext context, AnimationNotifyEndReason reason)
            {
                EndCount++;
                EndReason = reason;
            }
        }

        private sealed class GraphFixture : System.IDisposable
        {
            private readonly GameObject gameObject;

            public GraphFixture()
            {
                gameObject = new GameObject("PoseHandleCacheFixture");
                Animator = gameObject.AddComponent<Animator>();
                Graph = PlayableGraph.Create("PoseHandleCacheFixture");
                Context = new AnimationGraphContext(Animator, Graph);
            }

            public Animator Animator { get; }
            public PlayableGraph Graph { get; }
            public AnimationGraphContext Context { get; }

            public void Dispose()
            {
                if (Graph.IsValid()) Graph.Destroy();
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
