using CGame.Animation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CGame.Tests
{
    public class AnimationNodePlayableRuntimeTests
    {
        [Test]
        public void ClipNode_CreatesClipPlayable()
        {
            using (var fixture = new GraphFixture())
            {
                AnimationClip clip = CreateClip();
                var node = new ClipNode(clip, 1.25f);

                node.Initialize(fixture.Context);
                Playable playable = node.Evaluate(fixture.Context);

                Assert.IsTrue(node.IsInitialized);
                Assert.IsTrue(playable.IsValid());
                Assert.AreEqual(typeof(AnimationClipPlayable), playable.GetPlayableType());
                Assert.AreSame(clip, node.Clip);
                Assert.AreEqual(1.25d, playable.GetSpeed());
            }
        }

        [Test]
        public void Blend1DNode_ConnectsChildrenAndWeightsNearestSegment()
        {
            using (var fixture = new GraphFixture())
            {
                fixture.Context.MoveSpeed = 0.5f;
                var node = new Blend1DNode(
                    new[]
                    {
                        new Blend1DChild(new ClipNode(CreateClip()), 0f),
                        new Blend1DChild(new ClipNode(CreateClip()), 1f),
                        new Blend1DChild(new ClipNode(CreateClip()), 3f),
                    },
                    context => context.MoveSpeed);

                node.Initialize(fixture.Context);
                Playable playable = node.Evaluate(fixture.Context);

                Assert.IsTrue(playable.IsValid());
                Assert.AreEqual(typeof(AnimationMixerPlayable), playable.GetPlayableType());
                Assert.AreEqual(3, playable.GetInputCount());
                Assert.AreEqual(0.5f, playable.GetInputWeight(0));
                Assert.AreEqual(0.5f, playable.GetInputWeight(1));
                Assert.AreEqual(0f, playable.GetInputWeight(2));
            }
        }

        [Test]
        public void LayerBoneNode_ConnectsBaseAndOverlayWithMaskWeight()
        {
            using (var fixture = new GraphFixture())
            {
                fixture.Context.OverlayWeight = 0.75f;
                AvatarMask mask = new AvatarMask();
                var node = new LayerBoneNode(
                    new ClipNode(CreateClip()),
                    new ClipNode(CreateClip()),
                    mask);

                node.Initialize(fixture.Context);
                Playable playable = node.Evaluate(fixture.Context);

                Assert.IsTrue(playable.IsValid());
                Assert.AreEqual(typeof(AnimationLayerMixerPlayable), playable.GetPlayableType());
                Assert.AreEqual(2, playable.GetInputCount());
                Assert.AreEqual(1f, playable.GetInputWeight(0));
                Assert.AreEqual(0.75f, playable.GetInputWeight(1));
            }
        }

        [Test]
        public void LayerBoneNode_AvatarMaskOnlyAppliesOverlayToActiveBone()
        {
            GameObject character = new GameObject("MaskedCharacter");
            OutputNode outputNode = null;
            try
            {
                Animator animator = character.AddComponent<Animator>();
                Transform upperBody = CreateBone(character.transform, "UpperBody");
                Transform lowerBody = CreateBone(character.transform, "LowerBody");
                AnimationClip baseClip = CreateBoneClip(1f, 2f);
                AnimationClip overlayClip = CreateBoneClip(10f, 20f);
                AvatarMask upperBodyMask = CreateUpperBodyMask(character);
                var layerNode = new LayerBoneNode(
                    new ClipNode(baseClip),
                    new ClipNode(overlayClip),
                    upperBodyMask,
                    context => context.OverlayWeight);

                outputNode = new OutputNode(layerNode, "LayerBoneMaskTest");
                outputNode.Initialize(animator);
                outputNode.Context.OverlayWeight = 1f;
                outputNode.Update(0f);
                outputNode.Graph.Evaluate(0f);

                Assert.AreEqual(10f, upperBody.localPosition.x, 0.001f);
                Assert.AreEqual(2f, lowerBody.localPosition.x, 0.001f);
            }
            finally
            {
                outputNode?.Destroy();
                Object.DestroyImmediate(character);
            }
        }

        [Test]
        public void OutputNode_OwnsGraphAndDestroysIt()
        {
            GameObject gameObject = new GameObject("OutputNodeTest");
            OutputNode outputNode = null;
            try
            {
                Animator animator = gameObject.AddComponent<Animator>();
                outputNode = new OutputNode(new ClipNode(CreateClip()), "OutputNodeTestGraph");

                outputNode.Initialize(animator);
                outputNode.Update(0.016f);

                Assert.IsTrue(outputNode.IsInitialized);
                Assert.IsTrue(outputNode.Graph.IsValid());
                Assert.IsTrue(outputNode.SourcePlayable.IsValid());
                Assert.AreEqual(typeof(AnimationClipPlayable), outputNode.SourcePlayable.GetPlayableType());

                outputNode.Destroy();

                Assert.IsFalse(outputNode.IsInitialized);
                Assert.IsFalse(outputNode.Graph.IsValid());
            }
            finally
            {
                outputNode?.Destroy();
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ArtLocomotionAssets_BuildPlayableGraphOnRobotPrefab()
        {
            const string robotPrefabPath = "Assets/Art/Animation/FemaleLocomotionSet/Prefabs/Robot Kyle.prefab";
            GameObject robotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(robotPrefabPath);
            AnimationClipAsset idle = LoadClipAsset("Assets/Art/Animation/LocomotionAsset/InPlace/A_INP_IdleClipAsset.asset");
            AnimationClipAsset walk = LoadClipAsset("Assets/Art/Animation/LocomotionAsset/InPlace/A_INP_WalkFwd_LoopClipAsset.asset");
            AnimationClipAsset run = LoadClipAsset("Assets/Art/Animation/LocomotionAsset/InPlace/A_INP_RunFwd_LoopClipAsset.asset");

            Assert.IsNotNull(robotPrefab);
            Assert.IsTrue(idle.IsValid);
            Assert.IsTrue(walk.IsValid);
            Assert.IsTrue(run.IsValid);

            GameObject robot = (GameObject)PrefabUtility.InstantiatePrefab(robotPrefab);
            OutputNode outputNode = null;
            try
            {
                Animator animator = robot.GetComponentInChildren<Animator>();
                Assert.IsNotNull(animator);

                var locomotionNode = new Blend1DNode(
                    new[]
                    {
                        new Blend1DChild(new ClipNode(idle.AnimationClip), 0f),
                        new Blend1DChild(new ClipNode(walk.AnimationClip), 1f),
                        new Blend1DChild(new ClipNode(run.AnimationClip), 3f),
                    },
                    context => context.MoveSpeed);

                var layerNode = new LayerBoneNode(
                    locomotionNode,
                    new ClipNode(idle.AnimationClip),
                    new AvatarMask(),
                    context => context.OverlayWeight);

                outputNode = new OutputNode(layerNode, "ArtLocomotionNodeGraphTest");
                outputNode.Initialize(animator);
                outputNode.Context.MoveSpeed = 2f;
                outputNode.Context.OverlayWeight = 0.25f;
                outputNode.Update(0.016f);

                Assert.IsTrue(outputNode.Graph.IsValid());
                Assert.AreEqual(typeof(AnimationLayerMixerPlayable), outputNode.SourcePlayable.GetPlayableType());
                Assert.AreEqual(0.5f, locomotionNode.MixerPlayable.GetInputWeight(1));
                Assert.AreEqual(0.5f, locomotionNode.MixerPlayable.GetInputWeight(2));
                Assert.AreEqual(0.25f, layerNode.LayerMixerPlayable.GetInputWeight(1));
            }
            finally
            {
                outputNode?.Destroy();
                Object.DestroyImmediate(robot);
            }
        }

        private static AnimationClip CreateClip()
        {
            var clip = new AnimationClip
            {
                frameRate = 30f,
            };
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 1f));
            return clip;
        }

        private static Transform CreateBone(Transform parent, string name)
        {
            var bone = new GameObject(name);
            bone.transform.SetParent(parent, false);
            return bone.transform;
        }

        private static AnimationClip CreateBoneClip(float upperBodyPosition, float lowerBodyPosition)
        {
            var clip = new AnimationClip
            {
                frameRate = 30f,
            };
            clip.SetCurve("UpperBody", typeof(Transform), "localPosition.x", AnimationCurve.Constant(0f, 1f, upperBodyPosition));
            clip.SetCurve("LowerBody", typeof(Transform), "localPosition.x", AnimationCurve.Constant(0f, 1f, lowerBodyPosition));
            return clip;
        }

        private static AvatarMask CreateUpperBodyMask(GameObject character)
        {
            var mask = new AvatarMask();
            mask.AddTransformPath(character.transform, true);
            for (int i = 0; i < mask.transformCount; i++)
            {
                string path = mask.GetTransformPath(i);
                mask.SetTransformActive(i, path == string.Empty || path == "UpperBody");
            }

            return mask;
        }

        private static AnimationClipAsset LoadClipAsset(string assetPath)
        {
            AnimationClipAsset asset = AssetDatabase.LoadAssetAtPath<AnimationClipAsset>(assetPath);
            Assert.IsNotNull(asset, assetPath);
            Assert.IsNotNull(asset.AnimationClip, assetPath);
            return asset;
        }

        private sealed class GraphFixture : System.IDisposable
        {
            private readonly GameObject gameObject;

            public GraphFixture()
            {
                gameObject = new GameObject("AnimationNodeGraphFixture");
                Animator animator = gameObject.AddComponent<Animator>();
                Graph = PlayableGraph.Create("AnimationNodeGraphFixture");
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
