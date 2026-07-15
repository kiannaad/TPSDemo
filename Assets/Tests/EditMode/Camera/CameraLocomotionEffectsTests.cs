using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace CGame.Tests
{
    public sealed class CameraLocomotionEffectsTests
    {
        [Test]
        public void Evaluate_ProducesBoundedIndependentPoseDeltasWithoutUnityOwnership()
        {
            ScriptableObject profile = CreateProfile();
            object effects = CreateEffects(profile);
            object sample = Create("CGame.CameraLocomotionSample", 6f, 0f, true);

            IList contributions = EvaluateRepeated(effects, sample, 120, 1f / 60f);
            object bob = FindContribution(contributions, "Bob");
            object sway = FindContribution(contributions, "Sway");

            Assert.LessOrEqual(GetPosePosition(bob).magnitude, 0.05f);
            Assert.LessOrEqual(Quaternion.Angle(Quaternion.identity, GetPoseRotation(bob)), 1f);
            Assert.LessOrEqual(GetPosePosition(sway).magnitude, 0.05f);
            Assert.LessOrEqual(Quaternion.Angle(Quaternion.identity, GetPoseRotation(sway)), 1f);
            Assert.Greater(GetPoseWeight(bob), 0f);
            Assert.Greater(GetPoseWeight(sway), 0f);

            SetField(profile, "bobWeight", 0f);
            object zeroBobEffects = CreateEffects(profile);
            IList zeroBob = EvaluateRepeated(zeroBobEffects, sample, 120, 1f / 60f);
            Assert.AreEqual(0f, GetPoseWeight(FindContribution(zeroBob, "Bob")));
            Assert.Greater(GetPoseWeight(FindContribution(zeroBob, "Sway")), 0f);

            foreach (string typeName in new[] { "CGame.CameraLocomotionEffects", "CGame.CameraLocomotionSample" })
            {
                Type type = RequireRuntimeType(typeName);
                Assert.IsFalse(typeof(Component).IsAssignableFrom(type));
                Assert.IsFalse(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Any(field => field.FieldType == typeof(Transform)));
            }

            UnityEngine.Object.DestroyImmediate(profile);
        }

        [Test]
        public void StopAirborneLandingAndReset_SettleWithoutResidualPose()
        {
            ScriptableObject profile = CreateProfile();
            object effects = CreateEffects(profile);
            object moving = Create("CGame.CameraLocomotionSample", 6f, 0f, true);
            object idle = Create("CGame.CameraLocomotionSample", 0f, 0f, true);
            object airborne = Create("CGame.CameraLocomotionSample", 3f, 4f, false);

            EvaluateRepeated(effects, moving, 90, 1f / 60f);
            IList airResult = EvaluateRepeated(effects, airborne, 180, 1f / 60f);
            Assert.Less(GetPoseWeight(FindContribution(airResult, "Bob")), 0.01f);

            IList landed = EvaluateRepeated(effects, idle, 240, 1f / 60f);
            Assert.Less(GetPoseWeight(FindContribution(landed, "Bob")), 0.01f);
            Assert.Less(Mathf.Abs(GetPosePosition(FindContribution(landed, "Stance")).y), 0.001f);

            Invoke(effects, "Reset");
            IList reset = (IList)Invoke(effects, "Evaluate", idle, 0f);
            object composition = Compose(reset);
            object finalPose = GetProperty<object>(composition, "FinalPose");
            Assert.That(GetProperty<Vector3>(finalPose, "Position"), Is.EqualTo(new Vector3(1f, 2f, 3f)).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(GetProperty<Quaternion>(finalPose, "Rotation"), Is.EqualTo(Quaternion.Euler(15f, 25f, 0f)).Using(QuaternionEqualityComparer.Instance));

            UnityEngine.Object.DestroyImmediate(profile);
        }

        [Test]
        public void BaseAimRotation_RemainsImmediateAndEffectIsOnlyLocalDelta()
        {
            ScriptableObject profile = CreateProfile();
            object effects = CreateEffects(profile);
            IList contributions = EvaluateRepeated(
                effects,
                Create("CGame.CameraLocomotionSample", 4f, 0f, true),
                30,
                1f / 60f);
            object first = Compose(contributions, Quaternion.Euler(10f, 20f, 0f));
            object second = Compose(contributions, Quaternion.Euler(-20f, 110f, 0f));
            Quaternion firstBase = GetProperty<Quaternion>(GetProperty<object>(first, "BasePose"), "Rotation");
            Quaternion secondBase = GetProperty<Quaternion>(GetProperty<object>(second, "BasePose"), "Rotation");
            Quaternion firstFinal = GetProperty<Quaternion>(GetProperty<object>(first, "FinalPose"), "Rotation");
            Quaternion secondFinal = GetProperty<Quaternion>(GetProperty<object>(second, "FinalPose"), "Rotation");

            Assert.That(
                Quaternion.Inverse(firstBase) * firstFinal,
                Is.EqualTo(Quaternion.Inverse(secondBase) * secondFinal).Using(QuaternionEqualityComparer.Instance));

            UnityEngine.Object.DestroyImmediate(profile);
        }

        private static ScriptableObject CreateProfile()
        {
            return ScriptableObject.CreateInstance(RequireRuntimeType("CGame.CameraLocomotionEffectProfile"));
        }

        private static object CreateEffects(ScriptableObject profile)
        {
            return Activator.CreateInstance(
                RequireRuntimeType("CGame.CameraLocomotionEffects"),
                new object[] { profile });
        }

        private static IList EvaluateRepeated(object effects, object sample, int count, float deltaTime)
        {
            IList result = null;
            for (int index = 0; index < count; index++)
            {
                result = (IList)Invoke(effects, "Evaluate", sample, deltaTime);
            }

            return result;
        }

        private static object Compose(IList contributions, Quaternion? rotation = null)
        {
            Type contributionType = RequireRuntimeType("CGame.CameraLayerContribution");
            Array typed = Array.CreateInstance(contributionType, contributions.Count);
            contributions.CopyTo(typed, 0);
            return InvokeStatic(
                RequireRuntimeType("CGame.CameraPoseCompositor"),
                "Compose",
                Create("CGame.CameraPose", new Vector3(1f, 2f, 3f), rotation ?? Quaternion.Euler(15f, 25f, 0f)),
                Create("CGame.CameraLensState", 60f),
                typed);
        }

        private static object FindContribution(IList contributions, string layer)
        {
            return contributions.Cast<object>().Single(item => GetProperty<object>(item, "Layer").ToString() == layer);
        }

        private static float GetPoseWeight(object contribution)
        {
            return GetProperty<float>(GetProperty<object>(contribution, "PoseDelta"), "Weight");
        }

        private static Vector3 GetPosePosition(object contribution)
        {
            return GetProperty<Vector3>(GetProperty<object>(contribution, "PoseDelta"), "LocalPosition");
        }

        private static Quaternion GetPoseRotation(object contribution)
        {
            return GetProperty<Quaternion>(GetProperty<object>(contribution, "PoseDelta"), "LocalRotation");
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Field was not found: {fieldName}");
            field.SetValue(target, value);
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

        private static object InvokeStatic(Type type, string methodName, params object[] arguments)
        {
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
            Assert.NotNull(method, $"Method was not found: {methodName}");
            return method.Invoke(null, arguments);
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
