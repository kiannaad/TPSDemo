using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CGame.Tests
{
    public sealed class AdsPresentationContractTests
    {
        [Test]
        public void GameplayDecision_SeparatesAimIntentFromAuthorityResult()
        {
            Type decisionType = RequireRuntimeType("CGame.AimGameplayDecision");
            Type rejectionType = RequireRuntimeType("CGame.AimRejectionReason");
            object allowed = CreateDecision(decisionType, rejectionType, true, true, "None");
            object reloading = CreateDecision(decisionType, rejectionType, true, false, "Reloading");
            object released = CreateDecision(decisionType, rejectionType, false, false, "IntentReleased");

            Assert.IsTrue(decisionType.IsSealed);
            Assert.IsTrue(decisionType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .All(property => property.SetMethod == null));
            Assert.IsTrue(GetProperty<bool>(allowed, "AimHeld"));
            Assert.IsTrue(GetProperty<bool>(allowed, "IsAiming"));
            Assert.AreEqual("None", GetProperty<object>(allowed, "RejectionReason").ToString());

            Assert.IsTrue(GetProperty<bool>(reloading, "AimHeld"), "AimHeld remains input intent when gameplay rejects ADS.");
            Assert.IsFalse(GetProperty<bool>(reloading, "IsAiming"));
            Assert.AreEqual("Reloading", GetProperty<object>(reloading, "RejectionReason").ToString());

            Assert.IsFalse(GetProperty<bool>(released, "AimHeld"));
            Assert.IsFalse(GetProperty<bool>(released, "IsAiming"));
            Assert.AreEqual("IntentReleased", GetProperty<object>(released, "RejectionReason").ToString());

            TargetInvocationException invalidAllowed = Assert.Throws<TargetInvocationException>(() =>
                CreateDecision(decisionType, rejectionType, true, true, "Dead"));
            Assert.IsInstanceOf<ArgumentException>(invalidAllowed.InnerException);

            TargetInvocationException missingReason = Assert.Throws<TargetInvocationException>(() =>
                CreateDecision(decisionType, rejectionType, true, false, "None"));
            Assert.IsInstanceOf<ArgumentException>(missingReason.InnerException);
        }

        [Test]
        public void OwnerPresentation_OwnsOneProgressReadByAllConsumers()
        {
            Type decisionType = RequireRuntimeType("CGame.AimGameplayDecision");
            Type rejectionType = RequireRuntimeType("CGame.AimRejectionReason");
            Type readOnlyStateType = RequireRuntimeType("CGame.IAdsPresentationState");
            Type ownerStateType = RequireRuntimeType("CGame.OwnerAdsPresentationState");
            object ownerState = Activator.CreateInstance(ownerStateType);

            Assert.IsTrue(readOnlyStateType.IsInterface);
            Assert.IsTrue(readOnlyStateType.IsAssignableFrom(ownerStateType));
            Assert.IsTrue(readOnlyStateType.GetProperties().All(property => property.SetMethod == null));
            Assert.AreEqual(0f, GetProperty<float>(ownerState, "AdsProgress"));
            Assert.AreEqual("IntentReleased", GetProperty<object>(ownerState, "RejectionReason").ToString());
            TargetInvocationException nullDecision = Assert.Throws<TargetInvocationException>(() =>
                Invoke(ownerState, "ApplyDecision", new object[] { null }));
            Assert.IsInstanceOf<ArgumentNullException>(nullDecision.InnerException);

            object allowed = CreateDecision(decisionType, rejectionType, true, true, "None");
            Invoke(ownerState, "ApplyDecision", allowed);
            Invoke(ownerState, "Advance", 0.1f, 0.2f, 0.1f);

            Assert.AreEqual(0.5f, GetProperty<float>(ownerState, "AdsProgress"), 0.0001f);
            Assert.IsTrue(GetProperty<bool>(ownerState, "IsAiming"));

            object sprinting = CreateDecision(decisionType, rejectionType, true, false, "Sprinting");
            Invoke(ownerState, "ApplyDecision", sprinting);
            Invoke(ownerState, "Advance", 0.05f, 0.2f, 0.1f);

            Assert.AreEqual(0f, GetProperty<float>(ownerState, "AdsProgress"), 0.0001f);
            Assert.IsFalse(GetProperty<bool>(ownerState, "IsAiming"));
            Assert.AreEqual("Sprinting", GetProperty<object>(ownerState, "RejectionReason").ToString());

            object dead = CreateDecision(decisionType, rejectionType, true, false, "Dead");
            Invoke(ownerState, "ApplyDecision", dead);
            Invoke(ownerState, "Advance", 1f, 0.2f, 0.1f);
            Assert.AreEqual(0f, GetProperty<float>(ownerState, "AdsProgress"));
            Assert.AreEqual("Dead", GetProperty<object>(ownerState, "RejectionReason").ToString());
        }

        [Test]
        public void WeaponCameraProfile_StoresConfigurationWithoutRuntimeState()
        {
            Type profileType = RequireRuntimeType("CGame.WeaponCameraProfile");
            ScriptableObject profile = ScriptableObject.CreateInstance(profileType);

            try
            {
                Assert.Greater(GetProperty<float>(profile, "AdsWorldFieldOfView"), 0f);
                Assert.Greater(GetProperty<float>(profile, "AdsViewModelFieldOfView"), 0f);
                Assert.GreaterOrEqual(GetProperty<float>(profile, "AdsEnterDuration"), 0f);
                Assert.GreaterOrEqual(GetProperty<float>(profile, "AdsExitDuration"), 0f);
                Assert.That(GetProperty<float>(profile, "AdsLookSensitivityMultiplier"), Is.InRange(0f, 1f));
                Assert.AreNotEqual(Vector3.zero, GetProperty<Vector3>(profile, "AdsViewModelLocalPosition"));

                string[] memberNames = profileType
                    .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Select(member => member.Name)
                    .ToArray();
                CollectionAssert.DoesNotContain(memberNames, "AdsProgress");
                CollectionAssert.DoesNotContain(memberNames, "IsAiming");
                CollectionAssert.DoesNotContain(memberNames, "RejectionReason");
                Assert.IsTrue(profileType.GetProperties(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .All(property => property.SetMethod == null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void OwnerPresentation_InterruptsResumesAndResetsWithoutASecondProgress()
        {
            Type decisionType = RequireRuntimeType("CGame.AimGameplayDecision");
            Type rejectionType = RequireRuntimeType("CGame.AimRejectionReason");
            object state = Activator.CreateInstance(RequireRuntimeType("CGame.OwnerAdsPresentationState"));
            object allowed = CreateDecision(decisionType, rejectionType, true, true, "None");
            object reloading = CreateDecision(decisionType, rejectionType, true, false, "Reloading");

            Invoke(state, "ApplyDecision", allowed);
            Invoke(state, "Advance", 0.1f, 0.2f, 0.1f);
            Assert.AreEqual(0.5f, GetProperty<float>(state, "AdsProgress"), 0.0001f);

            Invoke(state, "ApplyDecision", reloading);
            Invoke(state, "Advance", 0.02f, 0.2f, 0.1f);
            Assert.AreEqual(0.3f, GetProperty<float>(state, "AdsProgress"), 0.0001f);

            Invoke(state, "ApplyDecision", allowed);
            Invoke(state, "Advance", 0.02f, 0.2f, 0.1f);
            Assert.AreEqual(0.4f, GetProperty<float>(state, "AdsProgress"), 0.0001f);

            Invoke(state, "Reset");
            Assert.AreEqual(0f, GetProperty<float>(state, "AdsProgress"));
            Assert.IsFalse(GetProperty<bool>(state, "IsAiming"));
            Assert.AreEqual("IntentReleased", GetProperty<object>(state, "RejectionReason").ToString());
        }

        private static object CreateDecision(
            Type decisionType,
            Type rejectionType,
            bool aimHeld,
            bool isAiming,
            string rejectionReason)
        {
            object reason = Enum.Parse(rejectionType, rejectionReason);
            return Activator.CreateInstance(decisionType, aimHeld, isAiming, reason);
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
