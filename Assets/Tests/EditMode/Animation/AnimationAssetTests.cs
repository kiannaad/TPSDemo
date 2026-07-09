using Animancer;
using CGame.Animation;
using NUnit.Framework;
using UnityEngine;

namespace CGame.Tests
{
    public class AnimationAssetTests
    {
        [Test]
        public void ClipAsset_CreatesPlayableClipTransition()
        {
            AnimationClip clip = new AnimationClip();
            AnimationClipAsset asset = ScriptableObject.CreateInstance<AnimationClipAsset>();
            asset.AnimationClip = clip;

            ITransition transition = asset.CreateTransition();

            Assert.IsTrue(asset.IsValid);
            Assert.IsInstanceOf<ClipTransition>(transition);
            Assert.AreSame(clip, ((ClipTransition)transition).Clip);
        }

        [Test]
        public void SequenceAsset_CreatesPlayableClipTransition()
        {
            AnimationClip clip = new AnimationClip();
            AnimationSequenceAsset asset = ScriptableObject.CreateInstance<AnimationSequenceAsset>();
            asset.AnimationClip = clip;

            ITransition transition = asset.CreateTransition();

            Assert.IsTrue(asset.IsValid);
            Assert.IsInstanceOf<ClipTransition>(transition);
            Assert.AreSame(clip, ((ClipTransition)transition).Clip);
        }

        [Test]
        public void TwoDimensionalBlend_ReferencesClipAssetsAndCreatesMixerTransition()
        {
            AnimationClip idleClip = new AnimationClip();
            AnimationClip moveClip = new AnimationClip();
            AnimationClipAsset idle = ScriptableObject.CreateInstance<AnimationClipAsset>();
            AnimationClipAsset move = ScriptableObject.CreateInstance<AnimationClipAsset>();
            idle.AnimationClip = idleClip;
            move.AnimationClip = moveClip;

            TwoDimensionalAnimationBlendAsset blend = ScriptableObject.CreateInstance<TwoDimensionalAnimationBlendAsset>();
            blend.Children = new[]
            {
                new TwoDimensionalAnimationBlendAsset.BlendChild
                {
                    ClipAsset = idle,
                    Threshold = Vector2.zero,
                },
                new TwoDimensionalAnimationBlendAsset.BlendChild
                {
                    ClipAsset = move,
                    Threshold = Vector2.up,
                },
            };

            MixerTransition2D transition = blend.CreateMixerTransition();

            Assert.IsTrue(blend.IsValid);
            Assert.AreEqual(2, transition.Animations.Length);
            Assert.AreSame(idleClip, transition.Animations[0]);
            Assert.AreSame(moveClip, transition.Animations[1]);
            Assert.AreEqual(Vector2.zero, transition.Thresholds[0]);
            Assert.AreEqual(Vector2.up, transition.Thresholds[1]);
        }

        [Test]
        public void AnimationAssetPlayer_PlaysAssetThroughAnimancerComponent()
        {
            GameObject gameObject = new GameObject("AnimationAssetPlayerTest");
            try
            {
                gameObject.AddComponent<Animator>();
                AnimancerComponent animancer = gameObject.AddComponent<AnimancerComponent>();
                AnimationAssetPlayer player = gameObject.AddComponent<AnimationAssetPlayer>();
                AnimationClipAsset asset = ScriptableObject.CreateInstance<AnimationClipAsset>();
                asset.AnimationClip = new AnimationClip();
                player.Animancer = animancer;
                player.AnimationAsset = asset;

                AnimancerState state = player.Play();

                Assert.IsNotNull(state);
                Assert.AreSame(asset.AnimationClip, state.MainObject);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AnimancerComponent_PlayAcceptsAnimationAssetDirectly()
        {
            GameObject gameObject = new GameObject("AnimancerComponentAnimationAssetExtensionTest");
            try
            {
                gameObject.AddComponent<Animator>();
                AnimancerComponent animancer = gameObject.AddComponent<AnimancerComponent>();
                AnimationClipAsset asset = ScriptableObject.CreateInstance<AnimationClipAsset>();
                asset.AnimationClip = new AnimationClip();

                AnimancerState state = animancer.Play(asset);

                Assert.IsNotNull(state);
                Assert.AreSame(asset.AnimationClip, state.MainObject);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
