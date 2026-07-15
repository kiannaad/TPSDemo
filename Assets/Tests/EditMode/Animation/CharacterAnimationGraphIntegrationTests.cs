using CGame.Animation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CGame.Tests
{
    public class CharacterAnimationGraphIntegrationTests
    {
        [Test]
        public void AnimInstance_ConsumesPhysicalSnapshotAndDrivesGroundAirStates()
        {
            CharacterAnimationConfig config = Resources.Load<CharacterAnimationConfig>("CharacterAnimationConfig");
            Assert.IsNotNull(config);
            Assert.IsTrue(config.IsValid);
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(LoadVisualPrefab());
            CharacterAnimInstance instance = null;
            try
            {
                Animator animator = visual.GetComponentInChildren<Animator>();
                instance = new CharacterAnimInstance(animator, config);

                Update(instance, Vector3.zero, true);
                Assert.AreEqual("Idle", instance.Graph.CurrentLocomotionState);

                Update(instance, new Vector3(0f, 0f, 2f), true);
                Assert.AreEqual("Move", instance.Graph.CurrentLocomotionState);

                Update(instance, new Vector3(0f, 5f, 1f), false);
                Assert.AreEqual("Air", instance.Graph.CurrentLocomotionState);

                Update(instance, new Vector3(0f, -2f, 1f), false);
                Assert.AreEqual("Air", instance.Graph.CurrentLocomotionState);
                Assert.AreEqual(-2f, instance.FrameData.WorldVelocity.y);

                Update(instance, Vector3.zero, true);
                Assert.AreEqual("Land", instance.Graph.CurrentLocomotionState);
                Assert.AreEqual(0f, instance.FrameData.WorldVelocity.y);
                Assert.IsTrue(instance.Graph.IsInitialized);
            }
            finally
            {
                instance?.Dispose();
                Object.DestroyImmediate(visual);
            }
        }

        [Test]
        public void CharacterGraph_DisposeAndRecreateLeavesFreshGraphState()
        {
            CharacterAnimationConfig config = Resources.Load<CharacterAnimationConfig>("CharacterAnimationConfig");
            GameObject firstVisual = (GameObject)PrefabUtility.InstantiatePrefab(LoadVisualPrefab());
            GameObject secondVisual = (GameObject)PrefabUtility.InstantiatePrefab(LoadVisualPrefab());
            CharacterAnimInstance first = null;
            CharacterAnimInstance second = null;
            try
            {
                first = new CharacterAnimInstance(firstVisual.GetComponentInChildren<Animator>(), config);
                Update(first, new Vector3(0f, 0f, 2f), true);
                CharacterAnimationGraph oldGraph = first.Graph;
                first.Dispose();
                first = null;
                Assert.IsFalse(oldGraph.IsInitialized);

                second = new CharacterAnimInstance(secondVisual.GetComponentInChildren<Animator>(), config);
                Update(second, Vector3.zero, true);
                Assert.IsTrue(second.Graph.IsInitialized);
                Assert.AreEqual("Idle", second.Graph.CurrentLocomotionState);
                Assert.AreEqual(Vector3.zero, second.Graph.Context.RootMotionDelta.PositionDelta);
                Assert.AreEqual(Quaternion.identity, second.Graph.Context.RootMotionDelta.RotationDelta);
            }
            finally
            {
                first?.Dispose();
                second?.Dispose();
                Object.DestroyImmediate(firstVisual);
                Object.DestroyImmediate(secondVisual);
            }
        }

        [Test]
        public void AnimInstance_EquipmentSnapshotDrivesWeaponLayerAndUnequipFallback()
        {
            CharacterAnimationConfig config = Resources.Load<CharacterAnimationConfig>("CharacterAnimationConfig");
            Assert.AreEqual(1, config.WeaponDefinitions.Length);
            WeaponAnimationDefinition definition = config.WeaponDefinitions[0];
            Assert.AreEqual(new WeaponId("rifle"), definition.WeaponId);
            Assert.IsTrue(definition.Idle.AnimationClip.isHumanMotion);
            Assert.IsTrue(definition.Walk.AnimationClip.isHumanMotion);
            Assert.IsTrue(definition.Run.AnimationClip.isHumanMotion);
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(LoadVisualPrefab());
            CharacterAnimInstance instance = null;
            try
            {
                instance = new CharacterAnimInstance(visual.GetComponentInChildren<Animator>(), config);
                Update(instance, Vector3.zero, true);

                instance.ApplyWeaponEquipment(new WeaponEquipmentSnapshot(new WeaponId("rifle"), 1u));
                Update(instance, Vector3.zero, true, 0.2f);

                Assert.AreEqual(new WeaponId("rifle"), instance.Graph.EquippedWeaponId);
                Assert.AreEqual(1u, instance.Graph.WeaponGeneration);
                Assert.IsNotNull(instance.Graph.WeaponLayerBlend.CurrentLayer);
                Assert.AreEqual("Idle", instance.Graph.WeaponLayerBlend.CurrentLayer.SelectedLocomotionState);

                Update(instance, new Vector3(0f, 0f, 2f), true);
                Assert.AreEqual("Move", instance.Graph.CurrentLocomotionState);
                Assert.AreEqual("Move", instance.Graph.WeaponLayerBlend.CurrentLayer.SelectedLocomotionState);

                instance.ApplyWeaponEquipment(new WeaponEquipmentSnapshot(default, 2u));
                Update(instance, Vector3.zero, true, 0.2f);
                Assert.IsNull(instance.Graph.WeaponLayerBlend.CurrentLayer);
                Assert.IsNull(instance.Graph.WeaponLayerBlend.NextLayer);
                Assert.IsFalse(instance.Graph.EquippedWeaponId.IsValid);
            }
            finally
            {
                instance?.Dispose();
                Object.DestroyImmediate(visual);
            }
        }

        [Test]
        public void CharacterConfig_ReferencesOnlyExpectedArtAssets()
        {
            CharacterAnimationConfig config = Resources.Load<CharacterAnimationConfig>("CharacterAnimationConfig");
            Assert.IsTrue(AssetDatabase.GetAssetPath(LoadVisualPrefab()).StartsWith("Assets/Art/"));
            AnimationClipAsset[] assets =
            {
                config.Idle, config.Walk, config.Run, config.Stop,
                config.JumpStart, config.InAir, config.Land,
            };
            foreach (AnimationClipAsset asset in assets)
            {
                Assert.IsTrue(AssetDatabase.GetAssetPath(asset).StartsWith("Assets/Art/"));
            }
        }

        private static void Update(CharacterAnimInstance instance, Vector3 velocity, bool grounded, float deltaTime = 0.016f)
        {
            bool jumping = !grounded && velocity.y > 0f;
            bool falling = !grounded && velocity.y <= 0f;
            var frame = new CharacterAnimationFrameData(
                Vector3.zero,
                Quaternion.identity,
                velocity,
                velocity,
                Vector3.zero,
                new Vector2(velocity.x, velocity.z).magnitude,
                0f,
                grounded,
                jumping,
                falling,
                jumping ? velocity.y / 9.81f : 0f);
            instance.UpdatePhysicalProperties(frame);
            instance.UpdateAnimation(deltaTime);
        }

        private static GameObject LoadVisualPrefab()
        {
            const string visualPrefabPath = "Assets/Art/Animation/FemaleLocomotionSet/Prefabs/Robot Kyle.prefab";
            GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(visualPrefabPath);
            Assert.IsNotNull(visualPrefab);
            return visualPrefab;
        }
    }
}
