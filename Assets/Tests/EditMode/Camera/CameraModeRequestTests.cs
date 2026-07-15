using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CGame.Tests
{
    public sealed class CameraModeRequestTests
    {
        [Test]
        public void HigherPriorityMode_WinsRegardlessOfRequestOrder()
        {
            object stack = Create("CGame.CameraModeRequestStack");
            object death = Request(stack, "Death", CreateTarget(new Vector3(1f, 2f, 3f)), "EaseInOut", 0.3f);
            object respawn = Request(stack, "Respawn", CreateTarget(new Vector3(4f, 5f, 6f)), "Cut", 0f);

            Assert.AreEqual("Death", GetProperty<object>(stack, "ActiveMode").ToString());
            Dispose(death);
            Assert.AreEqual("Respawn", GetProperty<object>(stack, "ActiveMode").ToString());
            Dispose(respawn);
            Assert.AreEqual("GameplayFirstPerson", GetProperty<object>(stack, "ActiveMode").ToString());
        }

        [Test]
        public void SamePriority_UsesNewestAndReleaseIsIdempotent()
        {
            object stack = Create("CGame.CameraModeRequestStack");
            object firstTarget = CreateTarget(new Vector3(1f, 0f, 0f));
            object secondTarget = CreateTarget(new Vector3(2f, 0f, 0f));
            object first = Request(stack, "Spectator", firstTarget, "EaseInOut", 0.25f);
            object second = Request(stack, "Spectator", secondTarget, "EaseInOut", 0.25f);

            Assert.AreSame(secondTarget, GetProperty<object>(GetProperty<object>(stack, "ActiveRequest"), "Target"));
            Dispose(second);
            Dispose(second);
            Assert.IsTrue(GetProperty<bool>(second, "IsReleased"));
            Assert.AreSame(firstTarget, GetProperty<object>(GetProperty<object>(stack, "ActiveRequest"), "Target"));
            Dispose(first);
        }

        [Test]
        public void InvalidTarget_IsReleasedAndFallsBackToNextRequest()
        {
            object stack = Create("CGame.CameraModeRequestStack");
            object deathTarget = CreateTarget(new Vector3(0f, 1f, 0f));
            object spectatorTarget = CreateTarget(new Vector3(0f, 2f, 0f));
            object death = Request(stack, "Death", deathTarget, "EaseInOut", 0.2f);
            object spectator = Request(stack, "Spectator", spectatorTarget, "EaseInOut", 0.2f);
            Assert.AreEqual("Spectator", GetProperty<object>(stack, "ActiveMode").ToString());

            Invoke(spectatorTarget, "Invalidating");

            Assert.AreEqual("Death", GetProperty<object>(stack, "ActiveMode").ToString());
            Assert.IsTrue(GetProperty<bool>(spectator, "IsReleased"));
            Dispose(death);
        }

        [Test]
        public void ClearAndDispose_RemoveAllRequestsWithoutChangingTargets()
        {
            object stack = Create("CGame.CameraModeRequestStack");
            object target = CreateTarget(new Vector3(3f, 4f, 5f));
            object request = Request(stack, "Cinematic", target, "EaseInOut", 0.4f);

            Invoke(stack, "Clear");
            Assert.AreEqual("GameplayFirstPerson", GetProperty<object>(stack, "ActiveMode").ToString());
            Assert.IsTrue(GetProperty<bool>(request, "IsReleased"));
            Assert.IsTrue(GetProperty<bool>(target, "IsValid"));
            ((IDisposable)stack).Dispose();
        }

        private static object CreateTarget(Vector3 position)
        {
            object target = Create("CGame.CameraModeTargetState");
            object pose = Create("CGame.CameraPose", position, Quaternion.Euler(5f, 15f, 0f));
            Invoke(target, "Updating", pose, 55f);
            return target;
        }

        private static object Request(object stack, string mode, object target, string transition, float duration)
        {
            Type modeType = RequireRuntimeType("CGame.CameraMode");
            Type transitionType = RequireRuntimeType("CGame.CameraModeTransition");
            object request = Create(
                "CGame.CameraModeRequest",
                Enum.Parse(modeType, mode),
                target,
                Enum.Parse(transitionType, transition),
                duration);
            return Invoke(stack, "Request", request);
        }

        private static void Dispose(object value)
        {
            ((IDisposable)value).Dispose();
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
