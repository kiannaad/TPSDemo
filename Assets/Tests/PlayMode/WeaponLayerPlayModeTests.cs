using System.Collections;
using CGame.Animation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CGame.Tests
{
    public class WeaponLayerPlayModeTests
    {
        [UnityTest]
        public IEnumerator RealArt_EquipMoveAndUnequipKeepPlayableGraphValid()
        {
            CharacterDefinition characterDefinition = Resources.Load<CharacterDefinition>("CharacterDefinition");
            CharacterAnimationConfig config = Resources.Load<CharacterAnimationConfig>("CharacterAnimationConfig");
            Assert.IsNotNull(characterDefinition);
            Assert.IsNotNull(config);
            Assert.AreEqual(1, config.WeaponDefinitions.Length);

            GameObject character = Object.Instantiate(characterDefinition.VisualPrefab);
            Animator animator = character.GetComponentInChildren<Animator>();
            WeaponAnimationDefinition definition = config.WeaponDefinitions[0];
            string locomotionState = "Idle";
            var blendNode = new WeaponLayerBlendNode(
                new ClipNode(config.Idle.AnimationClip),
                CreateUpperBodyMask());
            var output = new OutputNode(blendNode, "WeaponLayerPlayModeGraph");
            try
            {
                output.Initialize(animator);
                output.Update(0.016f);
                output.Graph.Evaluate(0.016f);
                yield return null;
                blendNode.SetTarget(new WeaponAnimationLayer(definition, 1u, () => locomotionState), 0.1f);
                output.Update(0.05f);
                output.Graph.Evaluate(0.05f);
                yield return null;
                output.Update(0.05f);
                output.Graph.Evaluate(0.05f);

                Assert.IsTrue(output.Graph.IsValid());
                Assert.IsNotNull(blendNode.CurrentLayer);
                Assert.AreEqual("Idle", blendNode.CurrentLayer.SelectedLocomotionState);
                for (int frame = 0; frame < 45; frame++)
                {
                    output.Update(0.033f);
                    output.Graph.Evaluate(0.033f);
                    yield return null;
                }
                AssertFeetRemainBelowHips(animator);

                locomotionState = "Move";
                output.Context.MoveSpeed = 2f;
                output.Update(0.016f);
                output.Graph.Evaluate(0.016f);
                yield return null;
                Assert.AreEqual("Move", blendNode.CurrentLayer.SelectedLocomotionState);
                Assert.IsTrue(blendNode.CurrentLayer.IsPoseAvailable);

                blendNode.SetTarget(null, 0.1f);
                output.Update(0.1f);
                output.Graph.Evaluate(0.1f);
                yield return null;
                Assert.IsNull(blendNode.CurrentLayer);
                Assert.IsTrue(output.Graph.IsValid());
            }
            finally
            {
                output.Destroy();
                Object.Destroy(character);
            }
        }

        private static void AssertFeetRemainBelowHips(Animator animator)
        {
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            Assert.IsNotNull(hips);
            Assert.IsNotNull(leftFoot);
            Assert.IsNotNull(rightFoot);
            Assert.Greater(hips.position.y, animator.transform.position.y + 0.5f,
                "The rifle layer must not collapse the humanoid hips to the character root.");
            Assert.Less(leftFoot.position.y, hips.position.y - 0.2f,
                "The rifle upper-body layer must not fold the left leg above the hips.");
            Assert.Less(rightFoot.position.y, hips.position.y - 0.2f,
                "The rifle upper-body layer must not fold the right leg above the hips.");
        }

        private static AvatarMask CreateUpperBodyMask()
        {
            var mask = new AvatarMask();
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            return mask;
        }
    }
}
