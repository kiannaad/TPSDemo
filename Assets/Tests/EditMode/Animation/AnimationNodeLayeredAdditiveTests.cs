using CGame.Animation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CGame.Tests
{
    public class AnimationNodeLayeredAdditiveTests
    {
        [Test]
        public void LayeredNode_PreservesInputOrderWeightsMasksAndAdditiveFlags()
        {
            using (var fixture = new GraphFixture())
            {
                AvatarMask mask = new AvatarMask();
                var node = new LayeredBlendPerBoneNode(
                    new ClipNode(CreateClip("", "localPosition.x", 1f)),
                    new LayeredAnimationInput(new ClipNode(CreateClip("", "localPosition.x", 2f)), mask, name: "Action"),
                    new LayeredAnimationInput(
                        new ClipNode(CreateClip("", "localPosition.y", 3f)),
                        mask,
                        context => context.LeftHandIkWeight,
                        true,
                        "AimAdditive"));
                node.Initialize(fixture.Context);
                fixture.Context.LeftHandIkWeight = 0.4f;
                node.Evaluate(fixture.Context);

                Assert.AreEqual(3, node.MixerPlayable.GetInputCount());
                Assert.AreEqual(1f, node.MixerPlayable.GetInputWeight(0));
                Assert.AreEqual(1f, node.MixerPlayable.GetInputWeight(1));
                Assert.AreEqual(0.4f, node.MixerPlayable.GetInputWeight(2), 0.001f);
                Assert.IsFalse(node.MixerPlayable.IsLayerAdditive(1));
                Assert.IsTrue(node.MixerPlayable.IsLayerAdditive(2));
            }
        }

        [Test]
        public void LayeredNode_ZeroWeightDoesNotAffectPoseAndLaterLayerHasStableOrder()
        {
            GameObject character = new GameObject("LayeredCharacter");
            GameObject upper = CreateBone(character.transform, "UpperBody");
            GameObject lower = CreateBone(character.transform, "LowerBody");
            OutputNode output = null;
            try
            {
                Animator animator = character.AddComponent<Animator>();
                AvatarMask upperMask = CreateUpperMask(character);
                var additive = new ClipNode(CreateAdditiveBodyClip(3f));
                var node = new LayeredBlendPerBoneNode(
                    new ClipNode(CreateBodyClip(1f, 2f, 0f)),
                    new LayeredAnimationInput(
                        new ClipNode(CreateBodyClip(10f, 20f, 0f)), upperMask, name: "FirstOverlay"),
                    new LayeredAnimationInput(
                        new ClipNode(CreateBodyClip(30f, 40f, 0f)), upperMask, context => context.OverlayWeight, name: "LaterOverlay"),
                    new LayeredAnimationInput(
                        additive, upperMask, context => context.LeftHandIkWeight, true, "Additive"));
                output = new OutputNode(node, "LayeredOrderTest");
                output.Initialize(animator);
                output.Context.OverlayWeight = 0f;
                output.Context.LeftHandIkWeight = 0f;
                output.Update(0f);
                output.Graph.Evaluate(0f);

                Assert.AreEqual(10f, upper.transform.localPosition.x, 0.001f);
                Assert.AreEqual(2f, lower.transform.localPosition.x, 0.001f);
                Assert.AreEqual(0f, upper.transform.localPosition.y, 0.001f);

                output.Context.OverlayWeight = 1f;
                output.Context.LeftHandIkWeight = 1f;
                additive.ClipPlayable.SetTime(0.5d);
                output.Update(0f);
                output.Graph.Evaluate(0f);

                Assert.AreEqual(30f, upper.transform.localPosition.x, 0.001f);
                Assert.AreEqual(2f, lower.transform.localPosition.x, 0.001f);
                Assert.AreEqual(1.5f, upper.transform.localPosition.y, 0.001f);
            }
            finally
            {
                output?.Destroy();
                Object.DestroyImmediate(character);
            }
        }

        [Test]
        public void AdditiveNode_UsesDedicatedAdditiveLayerAndWeight()
        {
            using (var fixture = new GraphFixture())
            {
                var node = new AdditiveNode(
                    new ClipNode(CreateClip("", "localPosition.x", 1f)),
                    new ClipNode(CreateClip("", "localPosition.y", 2f)),
                    weightGetter: context => context.OverlayWeight);
                node.Initialize(fixture.Context);
                fixture.Context.OverlayWeight = 0.6f;
                node.Evaluate(fixture.Context);

                Assert.IsTrue(node.MixerPlayable.IsLayerAdditive(1));
                Assert.AreEqual(1f, node.MixerPlayable.GetInputWeight(0));
                Assert.AreEqual(0.6f, node.MixerPlayable.GetInputWeight(1), 0.001f);
            }
        }

        [Test]
        public void ArtRobot_BuildsLocomotionActionAndAdditiveLayerChain()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Animation/FemaleLocomotionSet/Prefabs/Robot Kyle.prefab");
            AnimationClipAsset idle = LoadAsset("Assets/Art/Animation/LocomotionAsset/InPlace/A_INP_IdleClipAsset.asset");
            AnimationClipAsset walk = LoadAsset("Assets/Art/Animation/LocomotionAsset/InPlace/A_INP_WalkFwd_LoopClipAsset.asset");
            AnimationClipAsset run = LoadAsset("Assets/Art/Animation/LocomotionAsset/InPlace/A_INP_RunFwd_LoopClipAsset.asset");
            AnimationClipAsset action = LoadAsset("Assets/Art/Animation/LocomotionAsset/InPlace/A_INP_WalkFwd_Stop_FastClipAsset.asset");
            GameObject robot = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            OutputNode output = null;
            try
            {
                Animator animator = robot.GetComponentInChildren<Animator>();
                var locomotion = new Blend1DNode(new[]
                {
                    new Blend1DChild(new ClipNode(idle.AnimationClip), 0f),
                    new Blend1DChild(new ClipNode(walk.AnimationClip), 1f),
                    new Blend1DChild(new ClipNode(run.AnimationClip), 3f),
                }, context => context.MoveSpeed);
                var node = new LayeredBlendPerBoneNode(
                    new CachedPoseNode(locomotion),
                    new LayeredAnimationInput(new ClipNode(action.AnimationClip), new AvatarMask(), context => context.OverlayWeight, false, "UpperBodyAction"),
                    new LayeredAnimationInput(new ClipNode(idle.AnimationClip), new AvatarMask(), context => context.LeftHandIkWeight, true, "AimAdditive"));
                output = new OutputNode(node, "ArtLayeredAdditiveChain");
                output.Initialize(animator);
                output.Context.MoveSpeed = 2f;
                output.Context.OverlayWeight = 0.75f;
                output.Context.LeftHandIkWeight = 0.25f;
                output.Update(0.016f);

                Assert.IsTrue(output.Graph.IsValid());
                Assert.AreEqual(0.5f, locomotion.MixerPlayable.GetInputWeight(1), 0.001f);
                Assert.AreEqual(0.5f, locomotion.MixerPlayable.GetInputWeight(2), 0.001f);
                Assert.AreEqual(0.75f, node.MixerPlayable.GetInputWeight(1), 0.001f);
                Assert.AreEqual(0.25f, node.MixerPlayable.GetInputWeight(2), 0.001f);
                Assert.IsTrue(node.MixerPlayable.IsLayerAdditive(2));
            }
            finally
            {
                output?.Destroy();
                Object.DestroyImmediate(robot);
            }
        }

        private static GameObject CreateBone(Transform parent, string name)
        {
            var bone = new GameObject(name);
            bone.transform.SetParent(parent, false);
            return bone;
        }

        private static AnimationClip CreateClip(string path, string property, float value)
        {
            var clip = new AnimationClip { frameRate = 30f };
            clip.SetCurve(path, typeof(Transform), property, AnimationCurve.Constant(0f, 1f, value));
            return clip;
        }

        private static AnimationClip CreateBodyClip(float upperX, float lowerX, float upperY)
        {
            var clip = new AnimationClip { frameRate = 30f };
            clip.SetCurve("UpperBody", typeof(Transform), "localPosition.x", AnimationCurve.Constant(0f, 1f, upperX));
            clip.SetCurve("UpperBody", typeof(Transform), "localPosition.y", AnimationCurve.Constant(0f, 1f, upperY));
            clip.SetCurve("LowerBody", typeof(Transform), "localPosition.x", AnimationCurve.Constant(0f, 1f, lowerX));
            return clip;
        }

        private static AnimationClip CreateAdditiveBodyClip(float upperY)
        {
            var clip = new AnimationClip { frameRate = 30f };
            clip.SetCurve("UpperBody", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0f, 0f, 1f, upperY));
            return clip;
        }

        private static AvatarMask CreateUpperMask(GameObject character)
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
                gameObject = new GameObject("LayeredAdditiveFixture");
                Animator animator = gameObject.AddComponent<Animator>();
                Graph = PlayableGraph.Create("LayeredAdditiveFixture");
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
