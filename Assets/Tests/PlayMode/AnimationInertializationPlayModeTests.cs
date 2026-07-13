using System.Collections;
using CGame.Animation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Playables;

namespace CGame.Tests
{
    public class AnimationInertializationPlayModeTests
    {
        [UnityTest]
        public IEnumerator BoundTransformPrototype_SmoothsSourceJumpInPlayMode()
        {
            GameObject character = new GameObject("PlayModeInertializationCharacter");
            Transform hips = new GameObject("Hips").transform;
            hips.SetParent(character.transform, false);
            Animator animator = character.AddComponent<Animator>();
            var clip = new AnimationClip { frameRate = 30f };
            clip.SetCurve("Hips", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 10f));
            var clipNode = new ClipNode(clip, 0f);
            var inertialization = new InertializationNode(clipNode, hips);
            var output = new OutputNode(inertialization, "PlayModeInertialization");
            try
            {
                output.Initialize(animator);
                output.Graph.Evaluate(0f);
                clipNode.ClipPlayable.SetTime(0.9d);
                inertialization.Request(0.2f);
                output.Update(0f);
                output.Graph.Evaluate(0f);
                Assert.AreEqual(0f, hips.localPosition.x, 0.01f);

                output.Update(0.1f);
                output.Graph.Evaluate(0.1f);
                Assert.AreEqual(4.5f, hips.localPosition.x, 0.1f);
                yield return null;
            }
            finally
            {
                output.Destroy();
                Object.Destroy(character);
            }
        }
    }
}
