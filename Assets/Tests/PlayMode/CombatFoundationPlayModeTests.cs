using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CGame.Tests
{
    public sealed class CombatFoundationPlayModeTests
    {
        private GameObject target;
        private ScriptableObject profile;

        [TearDown]
        public void TearDown()
        {
            if (target != null)
            {
                UnityEngine.Object.DestroyImmediate(target);
            }

            if (profile != null)
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [UnityTest]
        public IEnumerator PlayerController_DrivesHitscanKillAndReloadThroughCombatIntent()
        {
            profile = CreateProfile();
            target = new GameObject("PlayerCombatTarget");
            target.transform.position = new Vector3(0f, 0f, 5f);
            target.AddComponent<BoxCollider>();
            Component health = target.AddComponent(RequireRuntimeType("CGame.HealthComponent"));
            Invoke(health, "Configure", "target", 50f);
            Physics.SyncTransforms();

            object resolver = Activator.CreateInstance(RequireRuntimeType("CGame.PhysicsWeaponHitResolver"));
            object weapon = Activator.CreateInstance(
                RequireRuntimeType("CGame.WeaponComponent"),
                profile,
                resolver,
                "local-player",
                7);
            object controller = Activator.CreateInstance(RequireRuntimeType("CGame.PlayerController"));
            object pawn = Activator.CreateInstance(RequireRuntimeType("CGame.Pawn"));
            Invoke(controller, "PossessingPawn", pawn);
            Invoke(controller, "SettingCombatIntentSink", weapon);

            PlayerInputState state = new PlayerInputState { FirePressed = true, FireHeld = true };
            Invoke(controller, "SettingInputStateProvider", new Func<PlayerInputState>(() => state));
            Invoke(controller, "UpdatingController", 0.016f);
            Invoke(weapon, "Advance", 0f, Vector3.zero);
            Assert.That(GetProperty<float>(health, "CurrentHealth"), Is.EqualTo(20f));

            yield return new WaitForFixedUpdate();
            Invoke(controller, "UpdatingController", 0.016f);
            Invoke(weapon, "Advance", 0.1f, Vector3.zero);
            Assert.IsTrue(GetProperty<bool>(health, "IsDead"));
            Assert.AreEqual(1, GetProperty<int>(weapon, "AmmoInMagazine"));

            state = new PlayerInputState { ReloadPressed = true };
            Invoke(controller, "UpdatingController", 0.016f);
            Invoke(weapon, "Advance", 0f, Vector3.zero);
            Assert.IsTrue(GetProperty<bool>(weapon, "IsReloading"));
            state = default;
            Invoke(controller, "UpdatingController", 0.016f);
            Invoke(weapon, "Advance", 0.5f, Vector3.zero);
            Assert.AreEqual(3, GetProperty<int>(weapon, "AmmoInMagazine"));

            Invoke(controller, "UnpossessingPawn");
            Invoke(weapon, "Advance", 1f, Vector3.zero);
            Assert.AreEqual(2, GetProperty<int>(weapon, "ShotsFired"));
        }

        private ScriptableObject CreateProfile()
        {
            ScriptableObject created = ScriptableObject.CreateInstance(RequireRuntimeType("CGame.WeaponProfile"));
            SetField(created, "magazineCapacity", 3);
            SetField(created, "secondsPerShot", 0.1f);
            SetField(created, "reloadDuration", 0.5f);
            SetField(created, "damage", 30f);
            SetField(created, "range", 100f);
            SetField(created, "spreadDegrees", 0f);
            SetField(created, "hitMask", (LayerMask)(~0));
            return created;
        }

        private static object Invoke(object targetObject, string methodName, params object[] arguments)
        {
            MethodInfo method = targetObject.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(method);
            return method.Invoke(targetObject, arguments);
        }

        private static T GetProperty<T>(object targetObject, string propertyName)
        {
            return (T)targetObject.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(targetObject);
        }

        private static void SetField(object targetObject, string fieldName, object value)
        {
            targetObject.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(targetObject, value);
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
