using System.IO;
using System.Linq;
using CGame.Animation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CGame.Tests
{
    public class ImportedWeaponArtTests
    {
        private const string WeaponArtRoot = "Assets/Art/Weapon/KINEMATION";

        [Test]
        public void CharacterAnimationFbx_AreHumanoidAndRootMotionLocked()
        {
            string[] paths = AssetDatabase.FindAssets("t:Model", new[] { WeaponArtRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".FBX", System.StringComparison.OrdinalIgnoreCase)
                    && path.Contains("/Animation/Character/"))
                .ToArray();

            Assert.AreEqual(82, paths.Length);
            foreach (string path in paths)
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                Assert.AreEqual(ModelImporterAnimationType.Human, importer.animationType, path);
                Assert.AreEqual(ModelImporterAvatarSetup.CopyFromOther, importer.avatarSetup, path);
                Assert.IsNotNull(importer.sourceAvatar, path);
                Assert.IsTrue(importer.sourceAvatar.isHuman, path);
                Assert.IsTrue(importer.sourceAvatar.isValid, path);

                AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<AnimationClip>()
                    .FirstOrDefault(asset => !asset.name.StartsWith("__preview__"));
                Assert.IsNotNull(clip, path);
                Assert.IsTrue(clip.isHumanMotion, path);
                Assert.AreEqual(Path.GetFileNameWithoutExtension(path), clip.name, path);

                ModelImporterClipAnimation settings = importer.clipAnimations.Single();
                Assert.IsTrue(settings.lockRootRotation, path);
                Assert.IsTrue(settings.lockRootHeightY, path);
                Assert.IsTrue(settings.lockRootPositionXZ, path);
                Assert.AreEqual(-0.9f, settings.heightOffset, 0.001f, path);
            }
        }

        [Test]
        public void WeaponModelFbx_AreRenderableAndUseValidMaterials()
        {
            string[] paths = AssetDatabase.FindAssets("t:Model", new[] { WeaponArtRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".FBX", System.StringComparison.OrdinalIgnoreCase)
                    && path.Contains("/Model/"))
                .ToArray();

            Assert.AreEqual(92, paths.Length);
            foreach (string path in paths)
            {
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.IsNotNull(model, path);
                Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
                Assert.IsNotEmpty(renderers, path);
                foreach (Material material in renderers.SelectMany(renderer => renderer.sharedMaterials))
                {
                    Assert.IsNotNull(material, path);
                    Assert.IsNotNull(material.shader, path + ":" + material.name);
                    Assert.AreNotEqual("Hidden/InternalErrorShader", material.shader.name, path + ":" + material.name);
                    if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null)
                    {
                        Assert.AreNotEqual(
                            "Universal Render Pipeline/Lit",
                            material.shader.name,
                            path + ":" + material.name + " is incompatible with the active Built-in Render Pipeline.");
                    }
                }
            }
        }

        [Test]
        public void RifleAkDefinition_UsesHumanoidArtAndPresentationAnchors()
        {
            const string definitionPath = "Assets/Art/Animation/Weapon/KINEMATION/AK/RifleAKAnimationDefinition.asset";
            const string prefabPath = "Assets/Art/Weapon/KINEMATION/AK/Prefabs/RifleAKPresentation.prefab";
            WeaponAnimationDefinition definition = AssetDatabase.LoadAssetAtPath<WeaponAnimationDefinition>(definitionPath);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            Assert.IsNotNull(definition);
            Assert.IsTrue(definition.IsValid);
            Assert.IsTrue(definition.Idle.AnimationClip.isHumanMotion);
            Assert.IsTrue(definition.Walk.AnimationClip.isHumanMotion);
            Assert.IsTrue(definition.Run.AnimationClip.isHumanMotion);
            Assert.IsNotNull(definition.Fire);
            Assert.IsTrue(definition.Fire.AnimationClip.isHumanMotion);
            Assert.AreSame(definition.Idle.AnimationClip, definition.Fire.AnimationClip);
            Assert.IsNotNull(definition.WeaponModelFire);
            Assert.AreEqual("A_W_AKX_Fire", definition.WeaponModelFire.name);
            Assert.IsTrue(AssetDatabase.GetAssetPath(definition.WeaponModelFire).StartsWith("Assets/Art/Weapon/KINEMATION/AK/"));
            Assert.AreSame(definition.Idle, definition.Stop);
            Assert.IsTrue(definition.HasPoseFor("Stop"));
            Assert.IsTrue(AssetDatabase.GetAssetPath(definition.Idle).StartsWith("Assets/Art/"));
            Assert.IsNotNull(prefab);
            Transform rightHandMount = prefab.transform.Find("RightHandMount");
            Assert.IsNotNull(rightHandMount);
            Assert.AreEqual(Vector3.zero, rightHandMount.localPosition);
            Assert.Less(Quaternion.Angle(Quaternion.identity, rightHandMount.localRotation), 0.01f);
            Assert.IsNotNull(prefab.transform.Find("LeftHandGrip"));
            Assert.IsNotNull(prefab.transform.Find("Muzzle"));
            Assert.IsNotEmpty(prefab.GetComponentsInChildren<Renderer>(true));
            Assert.IsNotNull(prefab.GetComponent<WeaponPresentationInstance>().ModelActionPlayer);
        }
    }
}
