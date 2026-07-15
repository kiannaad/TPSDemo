using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CGame.Tests
{
    public sealed class ObserverAimPresentationTests
    {
        [Test]
        public void Frame_ContainsOnlyStableBodyAimAndWeaponFacts()
        {
            Type frameType = RequireRuntimeType("CGame.Animation.ObserverAimFrame");
            string[] properties = frameType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(property => property.Name)
                .OrderBy(name => name)
                .ToArray();

            CollectionAssert.AreEqual(
                new[] { "AimPitch", "AimYaw", "BodyYaw", "WeaponState" },
                properties);
            Assert.IsFalse(properties.Any(name =>
                name.IndexOf("Camera", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Fov", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Recoil", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Bob", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Sway", StringComparison.OrdinalIgnoreCase) >= 0));
        }

        [Test]
        public void ObserverPitchLimit_IsIndependentFromOwnerCameraPitchLimit()
        {
            object controller = Create("CGame.Controller");
            Invoke(controller, "SettingPitchLimits", -89f, 89f);
            Invoke(controller, "SettingControlRotation", Quaternion.Euler(80f, 20f, 0f));
            Assert.AreEqual(80f, GetProperty<float>(controller, "ControlPitch"), 0.001f);

            object state = Create("CGame.Animation.ObserverAimPresentationState");
            Invoke(state, "Apply", CreateFrame(10f, 130f, 80f, "Ads"));
            Invoke(state, "Advance", 1f);
            object snapshot = GetProperty<object>(state, "Snapshot");

            Assert.AreEqual(60f, GetProperty<float>(snapshot, "AimPitch"), 0.001f);
            Assert.AreEqual(75f, GetProperty<float>(snapshot, "AimYawOffset"), 0.001f);
            Assert.AreEqual(10f, GetProperty<float>(snapshot, "BodyYaw"), 0.001f);
        }

        [Test]
        public void State_InterpolatesWeaponAndAimPresentationContinuously()
        {
            object state = Create("CGame.Animation.ObserverAimPresentationState");
            Invoke(state, "Apply", CreateFrame(0f, 0f, 0f, "HipFire"));
            Invoke(state, "Advance", 1f);
            Invoke(state, "Apply", CreateFrame(90f, 140f, 45f, "Ads"));
            Invoke(state, "Advance", 0.05f);
            object snapshot = GetProperty<object>(state, "Snapshot");

            Assert.That(GetProperty<float>(snapshot, "BodyYaw"), Is.InRange(0.01f, 89.99f));
            Assert.That(GetProperty<float>(snapshot, "AimPitch"), Is.InRange(0.01f, 44.99f));
            Assert.That(GetProperty<float>(snapshot, "AdsWeight"), Is.InRange(0.01f, 0.99f));
            Assert.IsTrue(GetProperty<bool>(snapshot, "WeaponVisible"));
            Assert.AreEqual("Ads", GetProperty<object>(snapshot, "WeaponState").ToString());
        }

        [Test]
        public void Clear_RemovesAimIkAndWeaponPresentationWithoutResidue()
        {
            object state = Create("CGame.Animation.ObserverAimPresentationState");
            Invoke(state, "Apply", CreateFrame(45f, 80f, -30f, "Reloading"));
            Invoke(state, "Advance", 1f);
            Invoke(state, "Clear");
            Invoke(state, "Advance", 1f);
            object snapshot = GetProperty<object>(state, "Snapshot");

            Assert.IsFalse(GetProperty<bool>(snapshot, "IsActive"));
            Assert.IsFalse(GetProperty<bool>(snapshot, "WeaponVisible"));
            Assert.AreEqual(0f, GetProperty<float>(snapshot, "AimWeight"));
            Assert.AreEqual(0f, GetProperty<float>(snapshot, "AdsWeight"));
            Assert.AreEqual(0f, GetProperty<float>(snapshot, "LeftHandIkWeight"));
        }

        private static object CreateFrame(float bodyYaw, float aimYaw, float aimPitch, string weaponState)
        {
            Type weaponStateType = RequireRuntimeType("CGame.Animation.ObserverWeaponState");
            return Create(
                "CGame.Animation.ObserverAimFrame",
                bodyYaw,
                aimYaw,
                aimPitch,
                Enum.Parse(weaponStateType, weaponState));
        }

        private static object Create(string typeName, params object[] arguments)
        {
            return Activator.CreateInstance(RequireRuntimeType(typeName), arguments);
        }

        private static object Invoke(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(method, $"Method was not found: {methodName}");
            return method.Invoke(target, arguments);
        }

        private static T GetProperty<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(property, $"Property was not found: {propertyName}");
            return (T)property.GetValue(target);
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
