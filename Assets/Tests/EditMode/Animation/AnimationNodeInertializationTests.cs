using CGame.Animation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;

namespace CGame.Tests
{
    public class AnimationNodeInertializationTests
    {
        [Test]
        public void InertializationNode_DecaysBoundTransformOffsetToNewSourcePose()
        {
            using (var fixture = new InertializationFixture())
            {
                fixture.Initialize();
                fixture.SampleSourceAt(0.9d);
                fixture.Node.Request(1f);

                fixture.Evaluate(0f);
                Assert.AreEqual(0f, fixture.Bone.localPosition.x, 0.001f);
                Assert.IsTrue(fixture.Node.IsActive);

                fixture.Evaluate(0.5f);
                Assert.AreEqual(4.5f, fixture.Bone.localPosition.x, 0.01f);

                fixture.Evaluate(0.5f);
                Assert.AreEqual(9f, fixture.Bone.localPosition.x, 0.01f);
                Assert.IsFalse(fixture.Node.IsActive);
            }
        }

        [TestCase(0f, true)]
        [TestCase(1f, false)]
        public void InertializationNode_ZeroDurationOrDisabledFallsBackToSourcePose(float duration, bool enabled)
        {
            using (var fixture = new InertializationFixture())
            {
                fixture.Initialize();
                fixture.Node.Enabled = enabled;
                fixture.SampleSourceAt(0.9d);
                fixture.Node.Request(duration);

                fixture.Evaluate(0f);

                Assert.AreEqual(9f, fixture.Bone.localPosition.x, 0.01f);
                Assert.IsFalse(fixture.Node.IsActive);
            }
        }

        [Test]
        public void InertializationNode_LowWeightFallsBackWithoutAffectingRootDelta()
        {
            using (var fixture = new InertializationFixture())
            {
                fixture.Initialize();
                fixture.SampleSourceAt(0.9d);
                fixture.Context.AccumulateRootMotionDelta(
                    new AnimationRootMotionDelta(new Vector3(0f, 0f, 0.25f), Quaternion.identity, 1f));
                fixture.Node.Request(1f, 0.001f);

                fixture.Evaluate(0f, resetRootDelta: false);

                Assert.AreEqual(9f, fixture.Bone.localPosition.x, 0.01f);
                Assert.AreEqual(0.25f, fixture.Context.RootMotionDelta.PositionDelta.z, 0.001f);
                Assert.IsFalse(fixture.Node.IsActive);
            }
        }

        private sealed class InertializationFixture : System.IDisposable
        {
            private readonly GameObject character;
            private readonly ClipNode clipNode;
            private OutputNode output;

            public InertializationFixture()
            {
                character = new GameObject("InertializationFixture");
                Bone = new GameObject("Hips").transform;
                Bone.SetParent(character.transform, false);
                Animator animator = character.AddComponent<Animator>();
                var clip = new AnimationClip { frameRate = 30f };
                clip.SetCurve("Hips", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 10f));
                clipNode = new ClipNode(clip, 0f);
                Node = new InertializationNode(clipNode, Bone, 0.01f);
                output = new OutputNode(Node, "InertializationFixture");
            }

            public Transform Bone { get; }
            public InertializationNode Node { get; }
            public AnimationGraphContext Context => output.Context;

            public void Initialize()
            {
                output.Initialize(character.GetComponent<Animator>());
                output.Graph.Evaluate(0f);
            }

            public void SampleSourceAt(double time)
            {
                clipNode.ClipPlayable.SetTime(time);
            }

            public void Evaluate(float deltaTime, bool resetRootDelta = true)
            {
                if (!resetRootDelta)
                {
                    AnimationRootMotionDelta delta = Context.RootMotionDelta;
                    output.Update(deltaTime);
                    Context.RootMotionDelta = delta;
                }
                else
                {
                    output.Update(deltaTime);
                }

                output.Graph.Evaluate(deltaTime);
            }

            public void Dispose()
            {
                output?.Destroy();
                Object.DestroyImmediate(character);
            }
        }
    }
}
