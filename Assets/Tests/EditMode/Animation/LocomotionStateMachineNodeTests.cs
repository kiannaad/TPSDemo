using System.Collections.Generic;
using CGame.Animation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;

namespace CGame.Tests
{
    public class LocomotionStateMachineNodeTests
    {
        [Test]
        public void IdleToMove_ReportsUpdateExitEnterEvaluateOrder()
        {
            using (var fixture = new GraphFixture())
            {
                LocomotionStateMachineNode node = CreateStateMachine();
                var phases = new List<string>();
                node.StatePhaseChanged += (state, phase) => phases.Add($"{state}.{phase}");
                node.Initialize(fixture.Context);
                phases.Clear();

                fixture.Context.MoveSpeed = 1f;
                node.Update(fixture.Context, 0.016f);
                node.Evaluate(fixture.Context);

                CollectionAssert.AreEqual(
                    new[]
                    {
                        "Idle.Update",
                        "Idle.Exit",
                        "Move.Enter",
                        "Move.Evaluate",
                    },
                    phases);
                Assert.AreEqual(LocomotionState.Move, node.CurrentState);
            }
        }

        [Test]
        public void MoveSpeed_TransitionsIdleMoveStopIdleWithLinearFade()
        {
            using (var fixture = new GraphFixture())
            {
                LocomotionStateMachineNode node = CreateStateMachine(0.1f, 0.2f, 0.1f);
                node.Initialize(fixture.Context);

                fixture.Context.MoveSpeed = 1f;
                node.Update(fixture.Context, 0f);
                node.Evaluate(fixture.Context);
                Assert.AreEqual(LocomotionState.Move, node.CurrentState);
                Assert.AreEqual(1f, node.MixerPlayable.GetInputWeight(0));
                Assert.AreEqual(0f, node.MixerPlayable.GetInputWeight(1));

                node.Update(fixture.Context, 0.05f);
                node.Evaluate(fixture.Context);
                Assert.AreEqual(0.5f, node.MixerPlayable.GetInputWeight(0), 0.001f);
                Assert.AreEqual(0.5f, node.MixerPlayable.GetInputWeight(1), 0.001f);

                node.Update(fixture.Context, 0.05f);
                node.Evaluate(fixture.Context);
                Assert.AreEqual(1f, node.MixerPlayable.GetInputWeight(1));

                fixture.Context.MoveSpeed = 0f;
                node.Update(fixture.Context, 0f);
                node.Evaluate(fixture.Context);
                Assert.AreEqual(LocomotionState.Stop, node.CurrentState);

                node.Update(fixture.Context, 0.2f);
                node.Evaluate(fixture.Context);
                Assert.AreEqual(LocomotionState.Idle, node.CurrentState);
            }
        }

        [Test]
        public void StopToMoveDuringFade_ReversesWithoutWeightJump()
        {
            using (var fixture = new GraphFixture())
            {
                LocomotionStateMachineNode node = CreateStateMachine(fadeDuration: 0.1f);
                node.Initialize(fixture.Context);
                fixture.Context.MoveSpeed = 1f;
                node.Update(fixture.Context, 0f);
                node.Update(fixture.Context, 0.1f);
                node.Evaluate(fixture.Context);

                fixture.Context.MoveSpeed = 0f;
                node.Update(fixture.Context, 0f);
                node.Update(fixture.Context, 0.04f);
                node.Evaluate(fixture.Context);
                float moveWeightBeforeReverse = node.MixerPlayable.GetInputWeight(1);
                float stopWeightBeforeReverse = node.MixerPlayable.GetInputWeight(2);

                fixture.Context.MoveSpeed = 1f;
                node.Update(fixture.Context, 0f);
                node.Evaluate(fixture.Context);

                Assert.AreEqual(LocomotionState.Move, node.CurrentState);
                Assert.AreEqual(moveWeightBeforeReverse, node.MixerPlayable.GetInputWeight(1), 0.001f);
                Assert.AreEqual(stopWeightBeforeReverse, node.MixerPlayable.GetInputWeight(2), 0.001f);
            }
        }

        [Test]
        public void ArtLocomotionAssets_BuildStateMachineAndBlendWalkRunOnRobotPrefab()
        {
            GameObject robotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Art/Animation/FemaleLocomotionSet/Prefabs/Robot Kyle.prefab");
            AnimationClipAsset idle = LoadClipAsset("Assets/Art/Animation/LocomotionAsset/InPlace/A_INP_IdleClipAsset.asset");
            AnimationClipAsset walk = LoadClipAsset("Assets/Art/Animation/LocomotionAsset/InPlace/A_INP_WalkFwd_LoopClipAsset.asset");
            AnimationClipAsset run = LoadClipAsset("Assets/Art/Animation/LocomotionAsset/InPlace/A_INP_RunFwd_LoopClipAsset.asset");
            AnimationClipAsset stop = LoadClipAsset("Assets/Art/Animation/LocomotionAsset/InPlace/A_INP_WalkFwd_Stop_FastClipAsset.asset");
            Assert.IsNotNull(robotPrefab);

            GameObject robot = (GameObject)PrefabUtility.InstantiatePrefab(robotPrefab);
            OutputNode outputNode = null;
            try
            {
                Animator animator = robot.GetComponentInChildren<Animator>();
                var moveNode = new Blend1DNode(
                    new[]
                    {
                        new Blend1DChild(new ClipNode(walk.AnimationClip), 1f),
                        new Blend1DChild(new ClipNode(run.AnimationClip), 3f),
                    },
                    context => context.MoveSpeed);
                var stateMachine = new LocomotionStateMachineNode(
                    new IdleState(new ClipNode(idle.AnimationClip)),
                    new MoveState(moveNode),
                    new StopState(new ClipNode(stop.AnimationClip)),
                    fadeDuration: 0.1f);

                outputNode = new OutputNode(stateMachine, "ArtLocomotionStateMachineTest");
                outputNode.Initialize(animator);
                outputNode.Context.MoveSpeed = 2f;
                outputNode.Update(0f);
                outputNode.Update(0.1f);

                Assert.AreEqual(LocomotionState.Move, stateMachine.CurrentState);
                Assert.AreEqual(0.5f, moveNode.MixerPlayable.GetInputWeight(0), 0.001f);
                Assert.AreEqual(0.5f, moveNode.MixerPlayable.GetInputWeight(1), 0.001f);
                Assert.AreEqual(1f, stateMachine.MixerPlayable.GetInputWeight(1));
            }
            finally
            {
                outputNode?.Destroy();
                Object.DestroyImmediate(robot);
            }
        }

        private static LocomotionStateMachineNode CreateStateMachine(
            float moveThreshold = 0.1f,
            float stopDuration = 0.2f,
            float fadeDuration = 0.1f)
        {
            return new LocomotionStateMachineNode(
                new IdleState(new ClipNode(CreateClip())),
                new MoveState(new ClipNode(CreateClip())),
                new StopState(new ClipNode(CreateClip())),
                moveThreshold,
                stopDuration,
                fadeDuration);
        }

        private static AnimationClip CreateClip()
        {
            var clip = new AnimationClip { frameRate = 30f };
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 1f));
            return clip;
        }

        private static AnimationClipAsset LoadClipAsset(string path)
        {
            AnimationClipAsset asset = AssetDatabase.LoadAssetAtPath<AnimationClipAsset>(path);
            Assert.IsNotNull(asset, path);
            Assert.IsTrue(asset.IsValid, path);
            return asset;
        }

        private sealed class GraphFixture : System.IDisposable
        {
            private readonly GameObject gameObject;

            public GraphFixture()
            {
                gameObject = new GameObject("LocomotionStateMachineFixture");
                Animator animator = gameObject.AddComponent<Animator>();
                Graph = PlayableGraph.Create("LocomotionStateMachineFixture");
                Context = new AnimationGraphContext(animator, Graph);
            }

            public PlayableGraph Graph { get; }
            public AnimationGraphContext Context { get; }

            public void Dispose()
            {
                if (Graph.IsValid())
                {
                    Graph.Destroy();
                }

                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
