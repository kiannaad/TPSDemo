using System.Collections.Generic;
using CGame.Animation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using AnimationState = CGame.Animation.AnimationState;

namespace CGame.Tests
{
    public class AnimationNodeGenericStateMachineTests
    {
        [Test]
        public void Transition_ReportsUpdateExitEnterEvaluateOrder()
        {
            using (var fixture = new GraphFixture())
            {
                AnimationStateMachineNode machine = CreateThreeStateMachine();
                var phases = new List<string>();
                machine.StatePhaseChanged += (state, phase) => phases.Add($"{state}.{phase}");
                machine.Initialize(fixture.Context);
                phases.Clear();
                fixture.Context.MoveSpeed = 1f;

                machine.Update(fixture.Context, 0f);
                machine.Evaluate(fixture.Context);

                CollectionAssert.AreEqual(new[]
                {
                    "Idle.Update",
                    "Idle.Exit",
                    "Move.Enter",
                    "Move.Evaluate",
                }, phases);
            }
        }

        [Test]
        public void CompetingTransitions_SelectHighestPriorityWithStableDeclarationOrder()
        {
            using (var fixture = new GraphFixture())
            {
                AnimationState[] states = CreateStates();
                var transitions = new[]
                {
                    new AnimationStateTransition("Idle", "Move", context => context.MoveSpeed > 0f, 1),
                    new AnimationStateTransition("Idle", "Stop", context => context.MoveSpeed > 0f, 10),
                };
                var machine = new AnimationStateMachineNode(states, transitions, "Idle");
                machine.Initialize(fixture.Context);
                fixture.Context.MoveSpeed = 1f;

                machine.Update(fixture.Context, 0f);

                Assert.AreEqual("Stop", machine.CurrentState);
                Assert.AreSame(transitions[1], machine.ActiveTransition);
            }
        }

        [Test]
        public void InterruptingTransition_StartsFromCurrentWeightVectorWithoutJump()
        {
            using (var fixture = new GraphFixture())
            {
                AnimationState[] states = CreateStates();
                var transitions = new[]
                {
                    new AnimationStateTransition("Idle", "Move", context => context.MoveSpeed > 0f, 1, 1f),
                    new AnimationStateTransition("Move", "Stop", context => context.OverlayWeight > 0f, 1, 0.5f),
                };
                var machine = new AnimationStateMachineNode(states, transitions, "Idle");
                machine.Initialize(fixture.Context);
                fixture.Context.MoveSpeed = 1f;
                machine.Update(fixture.Context, 0f);
                machine.Update(fixture.Context, 0.4f);
                float idleBefore = machine.MixerPlayable.GetInputWeight(0);
                float moveBefore = machine.MixerPlayable.GetInputWeight(1);

                fixture.Context.OverlayWeight = 1f;
                machine.Update(fixture.Context, 0f);

                Assert.AreEqual("Stop", machine.CurrentState);
                Assert.AreEqual(idleBefore, machine.MixerPlayable.GetInputWeight(0), 0.001f);
                Assert.AreEqual(moveBefore, machine.MixerPlayable.GetInputWeight(1), 0.001f);
                Assert.AreEqual(0f, machine.MixerPlayable.GetInputWeight(2), 0.001f);

                machine.Update(fixture.Context, 0.25f);
                Assert.AreEqual(idleBefore * 0.5f, machine.MixerPlayable.GetInputWeight(0), 0.001f);
                Assert.AreEqual(moveBefore * 0.5f, machine.MixerPlayable.GetInputWeight(1), 0.001f);
                Assert.AreEqual(0.5f, machine.MixerPlayable.GetInputWeight(2), 0.001f);
            }
        }

        [Test]
        public void NonInterruptibleTransition_BlocksCompetingTransitionUntilComplete()
        {
            using (var fixture = new GraphFixture())
            {
                AnimationState[] states = CreateStates();
                var transitions = new[]
                {
                    new AnimationStateTransition("Idle", "Move", context => context.MoveSpeed > 0f, 1, 0.2f, false),
                    new AnimationStateTransition("Move", "Stop", context => context.OverlayWeight > 0f, 100, 0.1f),
                };
                var machine = new AnimationStateMachineNode(states, transitions, "Idle");
                machine.Initialize(fixture.Context);
                fixture.Context.MoveSpeed = 1f;
                machine.Update(fixture.Context, 0f);
                fixture.Context.OverlayWeight = 1f;

                machine.Update(fixture.Context, 0.1f);
                Assert.AreEqual("Move", machine.CurrentState);

                machine.Update(fixture.Context, 0.1f);
                machine.Update(fixture.Context, 0f);
                Assert.AreEqual("Stop", machine.CurrentState);
            }
        }

        [Test]
        public void ArtLocomotionAssets_RunIdleMoveStopOnRobotWithGenericStateMachine()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Animation/FemaleLocomotionSet/Prefabs/Robot Kyle.prefab");
            AnimationClipAsset idle = LoadAsset("Assets/Art/Animation/LocomotionAsset/InPlace/A_INP_IdleClipAsset.asset");
            AnimationClipAsset walk = LoadAsset("Assets/Art/Animation/LocomotionAsset/InPlace/A_INP_WalkFwd_LoopClipAsset.asset");
            AnimationClipAsset run = LoadAsset("Assets/Art/Animation/LocomotionAsset/InPlace/A_INP_RunFwd_LoopClipAsset.asset");
            AnimationClipAsset stop = LoadAsset("Assets/Art/Animation/LocomotionAsset/InPlace/A_INP_WalkFwd_Stop_FastClipAsset.asset");
            GameObject robot = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            OutputNode output = null;
            try
            {
                Animator animator = robot.GetComponentInChildren<Animator>();
                var move = new Blend1DNode(new[]
                {
                    new Blend1DChild(new ClipNode(walk.AnimationClip), 1f),
                    new Blend1DChild(new ClipNode(run.AnimationClip), 3f),
                }, context => context.MoveSpeed);
                AnimationState[] states =
                {
                    new AnimationState("Idle", new ClipNode(idle.AnimationClip)),
                    new AnimationState("Move", move),
                    new AnimationState("Stop", new ClipNode(stop.AnimationClip)),
                };
                AnimationStateTransition[] transitions =
                {
                    new AnimationStateTransition("Idle", "Move", context => context.MoveSpeed > 0.1f),
                    new AnimationStateTransition("Move", "Stop", context => context.MoveSpeed <= 0.1f),
                    new AnimationStateTransition("Stop", "Move", context => context.MoveSpeed > 0.1f, 10),
                    new AnimationStateTransition("Stop", "Idle", context => context.MoveSpeed <= 0.1f, 0, 0.1f),
                };
                var machine = new AnimationStateMachineNode(states, transitions, "Idle");
                output = new OutputNode(machine, "ArtGenericLocomotionStateMachine");
                output.Initialize(animator);
                output.Context.MoveSpeed = 2f;
                output.Update(0f);
                output.Update(0.15f);
                Assert.AreEqual("Move", machine.CurrentState);
                Assert.AreEqual(0.5f, move.MixerPlayable.GetInputWeight(0), 0.001f);
                Assert.AreEqual(0.5f, move.MixerPlayable.GetInputWeight(1), 0.001f);

                output.Context.MoveSpeed = 0f;
                output.Update(0f);
                Assert.AreEqual("Stop", machine.CurrentState);
            }
            finally
            {
                output?.Destroy();
                Object.DestroyImmediate(robot);
            }
        }

        private static AnimationStateMachineNode CreateThreeStateMachine()
        {
            return new AnimationStateMachineNode(
                CreateStates(),
                new[]
                {
                    new AnimationStateTransition("Idle", "Move", context => context.MoveSpeed > 0f),
                    new AnimationStateTransition("Move", "Stop", context => context.MoveSpeed <= 0f),
                },
                "Idle");
        }

        private static AnimationState[] CreateStates()
        {
            return new[]
            {
                new AnimationState("Idle", new ClipNode(CreateClip())),
                new AnimationState("Move", new ClipNode(CreateClip())),
                new AnimationState("Stop", new ClipNode(CreateClip())),
            };
        }

        private static AnimationClip CreateClip()
        {
            var clip = new AnimationClip { frameRate = 30f };
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 1f));
            return clip;
        }

        private static AnimationClipAsset LoadAsset(string path)
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
                gameObject = new GameObject("GenericStateMachineFixture");
                Animator animator = gameObject.AddComponent<Animator>();
                Graph = PlayableGraph.Create("GenericStateMachineFixture");
                Context = new AnimationGraphContext(animator, Graph);
            }

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
