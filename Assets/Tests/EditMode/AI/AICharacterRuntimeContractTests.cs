using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CGame.Tests
{
    public sealed class AICharacterRuntimeContractTests
    {
        [Test]
        public void AIController_ConsumesOnlyBufferedControlFrameAndIntentSinks()
        {
            Type controllerType = RequireRuntimeType("CGame.AIController");
            Type frameType = RequireRuntimeType("CGame.AIControlFrame");

            CollectionAssert.AreEquivalent(
                new[] { "MovementDirection", "AimDirection", "JumpRequested", "FireRequested", "ReloadRequested" },
                frameType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(property => property.Name));
            Assert.NotNull(controllerType.GetMethod("SubmitControlFrame", new[] { frameType }));

            string[] forbiddenTypeNames = controllerType
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Select(field => field.FieldType.FullName)
                .Where(name => name != null)
                .Where(name => name.Contains("InputAction") || name.Contains("Motor") || name.Contains("Transform"))
                .ToArray();
            Assert.IsEmpty(forbiddenTypeNames);
        }

        [Test]
        public void AIController_SubmitsWorldMovementAndClearsItWhenReleased()
        {
            Type controllerType = RequireRuntimeType("CGame.AIController");
            Type pawnType = RequireRuntimeType("CGame.Pawn");
            Type frameType = RequireRuntimeType("CGame.AIControlFrame");
            object controller = Activator.CreateInstance(controllerType);
            object pawn = Activator.CreateInstance(pawnType);
            object frame = Activator.CreateInstance(
                frameType,
                Vector3.forward,
                Vector3.right,
                true,
                false,
                false);

            controllerType.GetMethod("PossessingPawn").Invoke(controller, new[] { pawn });
            controllerType.GetMethod("SubmitControlFrame").Invoke(controller, new[] { frame });
            controllerType.GetMethod("UpdatingController").Invoke(controller, new object[] { 0.02f });

            Assert.AreEqual(Vector3.forward, pawnType.GetMethod("PeekingMovementInput").Invoke(pawn, null));
            Quaternion rotation = (Quaternion)pawnType.GetProperty("ControlRotation").GetValue(pawn);
            Assert.Greater(Vector3.Dot(rotation * Vector3.forward, Vector3.right), 0.999f);

            controllerType.GetMethod("UnpossessingPawn").Invoke(controller, null);
            Assert.AreEqual(Vector3.zero, pawnType.GetMethod("PeekingMovementInput").Invoke(pawn, null));
        }

        [Test]
        public void SpawnBindings_SeparateLocalPlayerAndAIRuntimeOwnership()
        {
            Type bindingType = RequireRuntimeType("CGame.ICharacterControllerBinding");
            Type localBindingType = RequireRuntimeType("CGame.LocalPlayerControllerBinding");
            Type aiBindingType = RequireRuntimeType("CGame.AIControllerBinding");
            Type spawnManagerType = RequireRuntimeType("CGame.CharacterSpawnManager");

            Assert.IsTrue(bindingType.IsAssignableFrom(localBindingType));
            Assert.IsTrue(bindingType.IsAssignableFrom(aiBindingType));
            Assert.NotNull(spawnManagerType.GetMethod("TryGetAIRuntime", BindingFlags.Instance | BindingFlags.Public));
        }

        [Test]
        public void AIRuntimeRegistration_ExposesScriptControlAndSharedCombatFacts()
        {
            Type registrationType = RequireRuntimeType("CGame.AIRuntimeRegistration");
            Type frameType = RequireRuntimeType("CGame.AIControlFrame");

            foreach (string propertyName in new[] { "RuntimeId", "Controller", "Health", "WeaponRuntime", "Muzzle", "LeftHandSupport", "RightHandGrip", "IsActive" })
            {
                Assert.NotNull(registrationType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public), propertyName);
            }

            Assert.NotNull(registrationType.GetMethod("SubmitControlFrame", new[] { frameType }));
        }

        [Test]
        public void PrototypeLoadout_IsProjectOwnedAndDefinesRequiredRifleMarkers()
        {
            Type loadoutType = RequireRuntimeType("CGame.AIPrototypeLoadout");
            UnityEngine.Object loadout = Resources.Load("AIPrototypeLoadout", loadoutType);
            Assert.NotNull(loadout);
            Assert.IsTrue((bool)loadoutType.GetProperty("IsValid").GetValue(loadout));

            GameObject riflePrefab = (GameObject)loadoutType.GetProperty("RiflePrefab").GetValue(loadout);
            StringAssert.StartsWith("Assets/Art/Weapon/Prototype/", AssetDatabase.GetAssetPath(riflePrefab));
            Type viewType = RequireRuntimeType("CGame.PrototypeRifleView");
            Component view = riflePrefab.GetComponent(viewType);
            foreach (string propertyName in new[] { "ModelRoot", "RightHandGrip", "LeftHandSupport", "Muzzle" })
            {
                Transform marker = (Transform)viewType.GetProperty(propertyName).GetValue(view);
                Assert.NotNull(marker, propertyName);
            }

            Type definitionType = RequireRuntimeType("CGame.CharacterDefinition");
            Type controlKindType = RequireRuntimeType("CGame.CharacterControlKind");
            UnityEngine.Object definition = Resources.Load("CharacterDefinition", definitionType);
            object aiKind = Enum.Parse(controlKindType, "AI");
            Assert.IsTrue((bool)definitionType.GetMethod("Supports").Invoke(definition, new[] { aiKind }));
        }

        private static Type RequireRuntimeType(string fullName)
        {
            Type type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName))
                .FirstOrDefault(candidate => candidate != null);
            Assert.NotNull(type, $"Runtime type was not found: {fullName}");
            return type;
        }
    }
}
