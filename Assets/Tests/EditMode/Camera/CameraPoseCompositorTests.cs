using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace CGame.Tests
{
    public sealed class CameraPoseCompositorTests
    {
        [Test]
        public void Compose_UsesFixedLayerOrderAndRecordsEveryLayer()
        {
            object basePose = Create("CGame.CameraPose", Vector3.zero, Quaternion.identity);
            object baseLens = Create("CGame.CameraLensState", 60f);
            object stance = Contribution(
                "Stance",
                PoseDelta(Vector3.right, Quaternion.Euler(0f, 90f, 0f), 1f),
                LensDelta(0f, 0f));
            object bob = Contribution(
                "Bob",
                PoseDelta(Vector3.right, Quaternion.identity, 1f),
                LensDelta(0f, 0f));
            object lens = Contribution(
                "Lens",
                PoseDelta(Vector3.zero, Quaternion.identity, 0f),
                LensDelta(10f, 0.5f));

            object result = Compose(basePose, baseLens, lens, bob, stance);
            object finalPose = GetProperty<object>(result, "FinalPose");
            object finalLens = GetProperty<object>(result, "FinalLens");
            IList contributions = GetProperty<IList>(result, "Contributions");

            Assert.That(GetProperty<Vector3>(finalPose, "Position"), Is.EqualTo(new Vector3(1f, 0f, -1f)).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(GetProperty<float>(finalLens, "FieldOfView"), Is.EqualTo(65f).Within(0.0001f));
            CollectionAssert.AreEqual(
                new[] { "Stance", "Bob", "Sway", "VisualRecoil", "Impulse", "Lens" },
                contributions.Cast<object>().Select(item => GetProperty<object>(item, "Layer").ToString()).ToArray());
        }

        [Test]
        public void Compose_IsDeterministicAndZeroOrDisabledLayersReturnBase()
        {
            object basePose = Create("CGame.CameraPose", new Vector3(3f, 4f, 5f), Quaternion.Euler(10f, 20f, 30f));
            object baseLens = Create("CGame.CameraLensState", 72f);
            object disabled = Contribution("Bob", PoseDelta(Vector3.one, Quaternion.Euler(4f, 5f, 6f), 1f), LensDelta(8f, 1f), false);
            object zeroWeight = Contribution("Sway", PoseDelta(Vector3.one, Quaternion.Euler(7f, 8f, 9f), 0f), LensDelta(6f, 0f));

            object first = Compose(basePose, baseLens, disabled, zeroWeight);
            object second = Compose(basePose, baseLens, disabled, zeroWeight);

            AssertPoseAndLensEqual(first, second);
            Assert.That(GetProperty<Vector3>(GetProperty<object>(first, "FinalPose"), "Position"), Is.EqualTo(new Vector3(3f, 4f, 5f)).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(GetProperty<Quaternion>(GetProperty<object>(first, "FinalPose"), "Rotation"), Is.EqualTo(Quaternion.Euler(10f, 20f, 30f)).Using(QuaternionEqualityComparer.Instance));
            Assert.That(GetProperty<float>(GetProperty<object>(first, "FinalLens"), "FieldOfView"), Is.EqualTo(72f));
        }

        [Test]
        public void DebugSnapshot_ReadsExistingCompositionWithoutMutableEffectOrTransformOwnership()
        {
            object result = Compose(
                Create("CGame.CameraPose", Vector3.zero, Quaternion.identity),
                Create("CGame.CameraLensState", 60f),
                Contribution("Impulse", PoseDelta(Vector3.forward, Quaternion.identity, 0.25f), LensDelta(0f, 0f)));
            object snapshot = Create("CGame.CameraDebugSnapshot", true, result, 42);
            IList firstRead = GetProperty<IList>(snapshot, "Contributions");
            IList secondRead = GetProperty<IList>(snapshot, "Contributions");

            Assert.AreEqual(6, firstRead.Count);
            CollectionAssert.AreEqual(firstRead, secondRead);
            Assert.AreEqual(42, GetProperty<int>(snapshot, "Frame"));

            foreach (string typeName in new[] { "CGame.CameraPoseCompositor", "CGame.CameraPoseDelta", "CGame.CameraLensDelta" })
            {
                Type type = RequireRuntimeType(typeName);
                Assert.IsFalse(typeof(Component).IsAssignableFrom(type), $"{typeName} must not be a Unity component.");
                Assert.IsFalse(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Any(field => field.FieldType == typeof(Transform)));
                Assert.IsFalse(type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Any(property => property.PropertyType == typeof(Transform)));
            }
        }

        private static object Compose(object basePose, object baseLens, params object[] contributions)
        {
            Type contributionType = RequireRuntimeType("CGame.CameraLayerContribution");
            Array typedContributions = Array.CreateInstance(contributionType, contributions.Length);
            for (int index = 0; index < contributions.Length; index++)
            {
                typedContributions.SetValue(contributions[index], index);
            }

            return InvokeStatic(RequireRuntimeType("CGame.CameraPoseCompositor"), "Compose", basePose, baseLens, typedContributions);
        }

        private static object Contribution(string layerName, object poseDelta, object lensDelta, bool isEnabled = true)
        {
            Type layerType = RequireRuntimeType("CGame.CameraEffectLayer");
            return Create("CGame.CameraLayerContribution", Enum.Parse(layerType, layerName), poseDelta, lensDelta, isEnabled);
        }

        private static object PoseDelta(Vector3 position, Quaternion rotation, float weight)
        {
            return Create("CGame.CameraPoseDelta", position, rotation, weight);
        }

        private static object LensDelta(float fieldOfView, float weight)
        {
            return Create("CGame.CameraLensDelta", fieldOfView, weight);
        }

        private static void AssertPoseAndLensEqual(object first, object second)
        {
            object firstPose = GetProperty<object>(first, "FinalPose");
            object secondPose = GetProperty<object>(second, "FinalPose");
            Assert.That(GetProperty<Vector3>(firstPose, "Position"), Is.EqualTo(GetProperty<Vector3>(secondPose, "Position")).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(GetProperty<Quaternion>(firstPose, "Rotation"), Is.EqualTo(GetProperty<Quaternion>(secondPose, "Rotation")).Using(QuaternionEqualityComparer.Instance));
            Assert.That(GetProperty<float>(GetProperty<object>(first, "FinalLens"), "FieldOfView"), Is.EqualTo(GetProperty<float>(GetProperty<object>(second, "FinalLens"), "FieldOfView")));
        }

        private static object Create(string typeName, params object[] arguments)
        {
            return Activator.CreateInstance(RequireRuntimeType(typeName), arguments);
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
