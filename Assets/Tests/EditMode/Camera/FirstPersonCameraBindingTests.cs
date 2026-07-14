using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CGame.Tests
{
    public sealed class FirstPersonCameraBindingTests
    {
        private GameObject targetObject;

        [TearDown]
        public void TearDown()
        {
            if (targetObject != null)
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
                targetObject = null;
            }
        }

        [Test]
        public void BindUnbindAndDispose_AreSymmetricAndIdempotent()
        {
            Type bindingType = RequireRuntimeType("CGame.FirstPersonCameraBinding");
            Type anchorType = RequireRuntimeType("CGame.FirstPersonCameraAnchor");
            targetObject = new GameObject("CameraTarget");
            Component anchor = targetObject.AddComponent(anchorType);
            object binding = Activator.CreateInstance(bindingType);

            Assert.IsTrue((bool)Invoke(binding, "Bind", anchor));
            Assert.IsTrue((bool)GetProperty(binding, "IsBound"));
            Assert.AreSame(anchor, GetProperty(binding, "Target"));
            Assert.IsTrue((bool)Invoke(binding, "Bind", anchor));
            Assert.AreSame(anchor, GetProperty(binding, "Target"));

            Invoke(binding, "Unbind");
            Invoke(binding, "Unbind");
            Assert.IsFalse((bool)GetProperty(binding, "IsBound"));
            Assert.IsNull(GetProperty(binding, "Target"));

            Assert.IsTrue((bool)Invoke(binding, "Bind", anchor));
            Invoke(binding, "Dispose");
            Invoke(binding, "Dispose");
            Assert.IsFalse((bool)GetProperty(binding, "IsBound"));
            Assert.IsNull(GetProperty(binding, "Target"));
        }

        [Test]
        public void Profile_IsStaticConfigurationWithoutRuntimeBindingState()
        {
            Type profileType = RequireRuntimeType("CGame.FirstPersonCameraProfile");
            Assert.IsTrue(typeof(ScriptableObject).IsAssignableFrom(profileType));

            string[] runtimeStateNames =
            {
                "Yaw",
                "Pitch",
                "Target",
                "Binding",
                "CurrentYaw",
                "CurrentPitch",
                "EffectState"
            };
            string[] fieldNames = profileType
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(field => field.Name.TrimStart('_'))
                .ToArray();

            foreach (string runtimeStateName in runtimeStateNames)
            {
                Assert.IsFalse(
                    fieldNames.Any(fieldName => string.Equals(fieldName, runtimeStateName, StringComparison.OrdinalIgnoreCase)),
                    $"Profile must not own runtime state: {runtimeStateName}.");
            }

            Assert.NotNull(profileType.GetProperty("MinPitch", BindingFlags.Instance | BindingFlags.Public));
            Assert.NotNull(profileType.GetProperty("MaxPitch", BindingFlags.Instance | BindingFlags.Public));
            Assert.NotNull(profileType.GetProperty("BaseFieldOfView", BindingFlags.Instance | BindingFlags.Public));
        }

        [Test]
        public void BindCharacter_WithoutAnchorReturnsExplicitFailure()
        {
            Type bindingType = RequireRuntimeType("CGame.FirstPersonCameraBinding");
            targetObject = new GameObject("CharacterWithoutAnchor");
            object binding = Activator.CreateInstance(bindingType);
            MethodInfo bindCharacter = bindingType.GetMethod("BindCharacter", BindingFlags.Instance | BindingFlags.Public);

            Assert.NotNull(bindCharacter);
            object result = bindCharacter.Invoke(binding, new object[] { targetObject.transform });

            Assert.AreEqual("MissingAnchor", result.ToString());
            Assert.IsFalse((bool)GetProperty(binding, "IsBound"));
        }

        private static object Invoke(object target, string methodName, params object[] arguments)
        {
            return target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)
                ?.Invoke(target, arguments);
        }

        private static object GetProperty(object target, string propertyName)
        {
            return target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(target);
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
