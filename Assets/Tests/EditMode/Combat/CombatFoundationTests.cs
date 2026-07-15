using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CGame.Tests
{
    public sealed class CombatFoundationTests
    {
        private readonly List<UnityEngine.Object> objects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = objects.Count - 1; i >= 0; i--)
            {
                if (objects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(objects[i]);
                }
            }

            objects.Clear();
        }

        [Test]
        public void CharacterCombatIntent_ContainsOnlyAimFireAndReloadFacts()
        {
            Type intentType = RequireRuntimeType("CGame.CharacterCombatIntent");
            string[] propertyNames = intentType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(property => property.Name)
                .OrderBy(name => name)
                .ToArray();

            CollectionAssert.AreEqual(
                new[] { "AimDirection", "FireRequested", "ReloadRequested" },
                propertyNames);
            Assert.IsFalse(propertyNames.Any(name => name.Contains("Damage") || name.Contains("Hit")));
        }

        [Test]
        public void Weapon_FixedClockEnforcesCadenceMagazineAndReload()
        {
            ScriptableObject profile = CreateProfile(2, 0.5f, 1f, 30f, 100f, 0f);
            Component health = CreateHealth("target", 100f, new Vector3(0f, 0f, 5f));
            object weapon = CreateWeapon(profile, 10);
            object fireIntent = CreateIntent(Vector3.forward, true, false);

            Invoke(weapon, "SubmitCombatIntent", fireIntent);
            Advance(weapon, 0f);
            Assert.AreEqual(1, GetProperty<int>(weapon, "ShotsFired"));
            Assert.AreEqual(1, GetProperty<int>(weapon, "AmmoInMagazine"));
            Assert.That(GetProperty<float>(health, "CurrentHealth"), Is.EqualTo(70f));

            Advance(weapon, 0.1f);
            Assert.AreEqual(1, GetProperty<int>(weapon, "ShotsFired"));
            Advance(weapon, 0.4f);
            Assert.AreEqual(2, GetProperty<int>(weapon, "ShotsFired"));
            Assert.AreEqual(0, GetProperty<int>(weapon, "AmmoInMagazine"));
            Assert.That(GetProperty<float>(health, "CurrentHealth"), Is.EqualTo(40f));

            Advance(weapon, 1f);
            Assert.AreEqual(2, GetProperty<int>(weapon, "ShotsFired"));

            Invoke(weapon, "SubmitCombatIntent", CreateIntent(Vector3.forward, false, true));
            Advance(weapon, 0f);
            Assert.IsTrue(GetProperty<bool>(weapon, "IsReloading"));
            Advance(weapon, 0.5f);
            Assert.AreEqual(0, GetProperty<int>(weapon, "AmmoInMagazine"));
            Advance(weapon, 0.5f);
            Assert.IsFalse(GetProperty<bool>(weapon, "IsReloading"));
            Assert.AreEqual(2, GetProperty<int>(weapon, "AmmoInMagazine"));
        }

        [Test]
        public void Health_DeduplicatesDamageAndTransitionsToDeathOnce()
        {
            Component health = CreateHealth("target", 50f, Vector3.zero, false);
            int deathCount = 0;
            AddParameterIgnoringHandler(health, "Died", () => deathCount++);
            object firstDamage = CreateDamageEvent("damage-1", 20f);

            Assert.IsTrue((bool)Invoke(health, "ApplyDamage", firstDamage));
            Assert.IsFalse((bool)Invoke(health, "ApplyDamage", firstDamage));
            Assert.That(GetProperty<float>(health, "CurrentHealth"), Is.EqualTo(30f));

            object lethalDamage = CreateDamageEvent("damage-2", 40f);
            Assert.IsTrue((bool)Invoke(health, "ApplyDamage", lethalDamage));
            Assert.IsFalse((bool)Invoke(health, "ApplyDamage", CreateDamageEvent("damage-3", 40f)));
            Assert.IsTrue(GetProperty<bool>(health, "IsDead"));
            Assert.AreEqual(1, deathCount);
        }

        [Test]
        public void Weapon_SpreadIsRepeatableForEqualSeeds()
        {
            ScriptableObject profile = CreateProfile(2, 0.1f, 1f, 10f, 100f, 5f);
            object first = CreateWeapon(profile, 1234);
            object second = CreateWeapon(profile, 1234);
            object intent = CreateIntent(Vector3.forward, true, false);
            Invoke(first, "SubmitCombatIntent", intent);
            Invoke(second, "SubmitCombatIntent", intent);

            Advance(first, 0f);
            Advance(second, 0f);

            object firstShot = GetProperty<object>(first, "LastShot");
            object secondShot = GetProperty<object>(second, "LastShot");
            Assert.That(GetProperty<Vector3>(firstShot, "Direction"), Is.EqualTo(GetProperty<Vector3>(secondShot, "Direction")));
            Assert.That(Vector3.Angle(Vector3.forward, GetProperty<Vector3>(firstShot, "Direction")), Is.LessThan(8f));
        }

        private ScriptableObject CreateProfile(int capacity, float secondsPerShot, float reload, float damage, float range, float spread)
        {
            ScriptableObject profile = ScriptableObject.CreateInstance(RequireRuntimeType("CGame.WeaponProfile"));
            objects.Add(profile);
            SetField(profile, "magazineCapacity", capacity);
            SetField(profile, "secondsPerShot", secondsPerShot);
            SetField(profile, "reloadDuration", reload);
            SetField(profile, "damage", damage);
            SetField(profile, "range", range);
            SetField(profile, "spreadDegrees", spread);
            SetField(profile, "hitMask", (LayerMask)(~0));
            return profile;
        }

        private Component CreateHealth(string entityId, float maxHealth, Vector3 position, bool addCollider = true)
        {
            var target = new GameObject("CombatTarget");
            objects.Add(target);
            target.transform.position = position;
            if (addCollider)
            {
                target.AddComponent<BoxCollider>();
            }

            Component health = target.AddComponent(RequireRuntimeType("CGame.HealthComponent"));
            Invoke(health, "Configure", entityId, maxHealth);
            Physics.SyncTransforms();
            return health;
        }

        private static object CreateWeapon(ScriptableObject profile, int seed)
        {
            Type resolverType = RequireRuntimeType("CGame.PhysicsWeaponHitResolver");
            object resolver = Activator.CreateInstance(resolverType);
            return Activator.CreateInstance(
                RequireRuntimeType("CGame.WeaponComponent"),
                profile,
                resolver,
                "player",
                seed);
        }

        private static object CreateIntent(Vector3 direction, bool fire, bool reload)
        {
            return Activator.CreateInstance(RequireRuntimeType("CGame.CharacterCombatIntent"), direction, fire, reload);
        }

        private static object CreateDamageEvent(string eventId, float amount)
        {
            return Activator.CreateInstance(
                RequireRuntimeType("CGame.DamageEvent"),
                eventId,
                "source",
                "target",
                amount,
                Vector3.zero,
                Vector3.forward,
                1d);
        }

        private static void Advance(object weapon, float deltaTime)
        {
            Invoke(weapon, "Advance", deltaTime, Vector3.zero);
            Physics.SyncTransforms();
        }

        private static void AddParameterIgnoringHandler(object target, string eventName, Action callback)
        {
            EventInfo eventInfo = target.GetType().GetEvent(eventName, BindingFlags.Instance | BindingFlags.Public);
            ParameterExpression[] parameters = eventInfo.EventHandlerType.GetMethod("Invoke")
                .GetParameters()
                .Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name))
                .ToArray();
            MethodCallExpression body = Expression.Call(Expression.Constant(callback.Target), callback.Method);
            Delegate handler = Expression.Lambda(eventInfo.EventHandlerType, body, parameters).Compile();
            eventInfo.AddEventHandler(target, handler);
        }

        private static object Invoke(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(method, $"Method not found: {target.GetType().FullName}.{methodName}");
            return method.Invoke(target, arguments);
        }

        private static T GetProperty<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(property, $"Property not found: {target.GetType().FullName}.{propertyName}");
            return (T)property.GetValue(target);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Field not found: {target.GetType().FullName}.{fieldName}");
            field.SetValue(target, value);
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
