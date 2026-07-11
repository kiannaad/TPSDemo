using System;
using CGame.Animation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CGame.Tests
{
    public class AnimationNodeActionEventsIkRootTests
    {
        [Test]
        public void PriorityNode_ReloadInterruptsFireAndEndsDurationNotify()
        {
            using (var fixture = new GraphFixture())
            {
                var notify = new RecordingDurationNotify();
                var instantNotify = new RecordingInstantNotify();
                AnimationClipAsset fireAsset = CreateClipAsset(CreateClip(1f));
                AnimationNotifyTrack notifyTrack = fireAsset.AddNotifyTrack();
                notifyTrack.AddEvent(notify, 1, 20);
                notifyTrack.AddEvent(instantNotify, 2);
                AnimationClipAsset reloadAsset = CreateClipAsset(CreateClip(1f));
                var fire = new ActionNode(fireAsset, 10);
                var reload = new ActionNode(reloadAsset, 20);
                var priority = new PriorityNode(fire, reload);
                priority.Initialize(fixture.Context);

                fire.Request();
                priority.Update(fixture.Context, 0f);
                fire.ClipPlayable.SetTime(0.1d);
                priority.Update(fixture.Context, 0.1f);
                Assert.AreEqual(1, notify.BeginCount);
                Assert.AreEqual(1, instantNotify.Count);

                reload.Request();
                priority.Update(fixture.Context, 0f);

                Assert.AreSame(reload, priority.ActiveAction);
                Assert.IsFalse(fire.IsActive);
                Assert.AreEqual(1, notify.EndCount);
                Assert.AreEqual(AnimationNotifyEndReason.Interrupted, notify.LastEndReason);
                UnityEngine.Object.DestroyImmediate(fireAsset);
                UnityEngine.Object.DestroyImmediate(reloadAsset);
            }
        }

        [Test]
        public void LeftHandIkNode_MovesOnlyBoundHandTowardTarget()
        {
            GameObject character = new GameObject("IkCharacter");
            GameObject handObject = new GameObject("LeftHand");
            GameObject targetObject = new GameObject("LeftHandTarget");
            OutputNode output = null;
            try
            {
                handObject.transform.SetParent(character.transform, false);
                targetObject.transform.position = new Vector3(2f, 3f, 4f);
                targetObject.transform.rotation = Quaternion.Euler(10f, 20f, 30f);
                Animator animator = character.AddComponent<Animator>();
                var ikNode = new LeftHandIkNode(new ClipNode(CreateClip(1f)), handObject.transform, targetObject.transform);
                output = new OutputNode(ikNode, "LeftHandIkTest");
                output.Initialize(animator);
                output.Context.LeftHandIkWeight = 1f;
                output.Update(0f);
                output.Graph.Evaluate(0f);

                Assert.AreEqual(targetObject.transform.position.x, handObject.transform.position.x, 0.001f);
                Assert.AreEqual(targetObject.transform.position.y, handObject.transform.position.y, 0.001f);
                Assert.AreEqual(targetObject.transform.position.z, handObject.transform.position.z, 0.001f);
            }
            finally
            {
                output?.Destroy();
                UnityEngine.Object.DestroyImmediate(targetObject);
                UnityEngine.Object.DestroyImmediate(character);
            }
        }

        [Test]
        public void RootDeltaNode_OutputsCapturedDeltaWithoutMovingOwnerTransform()
        {
            using (var fixture = new GraphFixture())
            {
                var node = new RootDeltaNode(new ClipNode(CreateClip(1f)));
                node.Initialize(fixture.Context);
                Vector3 startingPosition = fixture.Animator.transform.position;
                var job = new RootMotionCaptureJob
                {
                    PositionDelta = new Vector3(0.25f, 0f, 0.5f),
                    RotationDelta = Quaternion.Euler(0f, 15f, 0f),
                };
                node.ScriptPlayable.SetJobData(job);

                node.Update(fixture.Context, 0.016f);

                Assert.AreEqual(new Vector3(0.25f, 0f, 0.5f), fixture.Context.RootMotionDelta.PositionDelta);
                Assert.AreEqual(1f, fixture.Context.RootMotionDelta.SourceWeight);
                Assert.AreEqual(startingPosition, fixture.Animator.transform.position);

                fixture.Context.AccumulateRootMotionDelta(
                    new AnimationRootMotionDelta(new Vector3(0f, 0f, 1f), Quaternion.identity, 0.5f));
                Assert.AreEqual(1f, fixture.Context.RootMotionDelta.PositionDelta.z, 0.001f);
            }
        }

        [Test]
        public void ArtRobot_BuildsLocomotionActionIkRootOutputChain()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Animation/FemaleLocomotionSet/Prefabs/Robot Kyle.prefab");
            AnimationClipAsset idle = LoadAsset("Assets/Art/Animation/LocomotionAsset/InPlace/A_INP_IdleClipAsset.asset");
            AnimationClipAsset walk = LoadAsset("Assets/Art/Animation/LocomotionAsset/InPlace/A_INP_WalkFwd_LoopClipAsset.asset");
            AnimationClipAsset run = LoadAsset("Assets/Art/Animation/LocomotionAsset/InPlace/A_INP_RunFwd_LoopClipAsset.asset");
            AnimationClipAsset stop = LoadAsset("Assets/Art/Animation/LocomotionAsset/InPlace/A_INP_WalkFwd_Stop_FastClipAsset.asset");
            GameObject robot = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            GameObject target = new GameObject("ArtLeftHandTarget");
            OutputNode output = null;
            try
            {
                Animator animator = robot.GetComponentInChildren<Animator>();
                Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                Assert.IsNotNull(leftHand);
                target.transform.position = leftHand.position;
                target.transform.rotation = leftHand.rotation;
                var move = new Blend1DNode(new[]
                {
                    new Blend1DChild(new ClipNode(walk.AnimationClip), 1f),
                    new Blend1DChild(new ClipNode(run.AnimationClip), 3f),
                }, context => context.MoveSpeed);
                var locomotion = new LocomotionStateMachineNode(
                    new IdleState(new ClipNode(idle.AnimationClip)),
                    new MoveState(move),
                    new StopState(new ClipNode(stop.AnimationClip)));
                var fire = new ActionNode(idle, 10);
                var reload = new ActionNode(stop, 20);
                var actions = new PriorityNode(fire, reload);
                var layered = new LayerBoneNode(locomotion, actions, new AvatarMask(), context => context.OverlayWeight);
                var ik = new LeftHandIkNode(layered, leftHand, target.transform);
                var root = new RootDeltaNode(ik);
                output = new OutputNode(root, "ArtV3AnimationNodeChain");
                output.Initialize(animator);
                output.Context.MoveSpeed = 2f;
                output.Context.OverlayWeight = 1f;
                output.Context.LeftHandIkWeight = 1f;
                reload.Request();
                output.Update(0.016f);

                Assert.IsTrue(output.Graph.IsValid());
                Assert.AreEqual(LocomotionState.Move, locomotion.CurrentState);
                Assert.AreSame(reload, actions.ActiveAction);
                Assert.AreEqual(typeof(AnimationScriptPlayable), output.SourcePlayable.GetPlayableType());
            }
            finally
            {
                output?.Destroy();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(robot);
            }
        }

        private static AnimationClip CreateClip(float length)
        {
            var clip = new AnimationClip { frameRate = 30f };
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, length, 1f));
            return clip;
        }

        private static AnimationClipAsset CreateClipAsset(AnimationClip clip)
        {
            AnimationClipAsset asset = ScriptableObject.CreateInstance<AnimationClipAsset>();
            Assert.IsTrue(asset.TryInitialize(clip));
            return asset;
        }

        private static AnimationClipAsset LoadAsset(string path)
        {
            AnimationClipAsset asset = AssetDatabase.LoadAssetAtPath<AnimationClipAsset>(path);
            Assert.IsNotNull(asset, path);
            Assert.IsTrue(asset.IsValid, path);
            return asset;
        }

        [Serializable]
        private sealed class RecordingDurationNotify : AnimationDurationNotify
        {
            public int BeginCount { get; private set; }
            public int EndCount { get; private set; }
            public AnimationNotifyEndReason LastEndReason { get; private set; }

            public override void OnBegin(AnimationEventContext context) => BeginCount++;

            public override void OnEnd(AnimationEventContext context, AnimationNotifyEndReason reason)
            {
                EndCount++;
                LastEndReason = reason;
            }
        }

        [Serializable]
        private sealed class RecordingInstantNotify : AnimationInstantNotify
        {
            public int Count { get; private set; }

            public override void OnNotify(AnimationEventContext context) => Count++;
        }

        private sealed class GraphFixture : IDisposable
        {
            private readonly GameObject gameObject;

            public GraphFixture()
            {
                gameObject = new GameObject("V3AnimationNodeFixture");
                Animator = gameObject.AddComponent<Animator>();
                Graph = PlayableGraph.Create("V3AnimationNodeFixture");
                Context = new AnimationGraphContext(Animator, Graph);
            }

            public Animator Animator { get; }
            public PlayableGraph Graph { get; }
            public AnimationGraphContext Context { get; }

            public void Dispose()
            {
                if (Graph.IsValid()) Graph.Destroy();
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }
}
