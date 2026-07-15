using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace CGame.Tests
{
    public sealed class RecoilSeparationTests
    {
        [Test]
        public void GameplayRecoil_IsDeterministicAndRecoversThroughAimState()
        {
            object first = Create("CGame.GameplayRecoilState");
            object second = Create("CGame.GameplayRecoilState");
            Vector2 kick = new Vector2(-1.2f, 0.25f);

            Invoke(first, "ApplyKick", kick, 9f);
            Invoke(second, "ApplyKick", kick, 9f);
            for (int index = 0; index < 180; index++)
            {
                Vector2 firstDelta = (Vector2)Invoke(first, "Advance", 1f / 60f);
                Vector2 secondDelta = (Vector2)Invoke(second, "Advance", 1f / 60f);
                Assert.That(firstDelta, Is.EqualTo(secondDelta).Using(Vector2ComparerWithEqualsOperator.Instance));
                Assert.That(
                    GetProperty<Vector2>(first, "Offset"),
                    Is.EqualTo(GetProperty<Vector2>(second, "Offset")).Using(Vector2ComparerWithEqualsOperator.Instance));
            }

            Assert.That(GetProperty<Vector2>(first, "Offset"), Is.EqualTo(Vector2.zero).Using(Vector2ComparerWithEqualsOperator.Instance));
        }

        [Test]
        public void ControllerAimDirection_DependsOnGameplayKickButNotVisualKick()
        {
            object firstController = Create("CGame.Controller");
            object secondController = Create("CGame.Controller");
            Invoke(firstController, "SettingControlRotation", Quaternion.Euler(5f, 35f, 0f));
            Invoke(secondController, "SettingControlRotation", Quaternion.Euler(5f, 35f, 0f));
            object firstRequest = CreateRequest(new Vector3(0f, 0f, -0.02f), new Vector3(-0.8f, 0.1f, 0f));
            object secondRequest = CreateRequest(Vector3.zero, Vector3.zero);

            ApplyGameplayFromRequest(firstController, firstRequest);
            ApplyGameplayFromRequest(secondController, secondRequest);
            Quaternion firstAim = GetProperty<Quaternion>(firstController, "ControlRotation");
            Quaternion secondAim = GetProperty<Quaternion>(secondController, "ControlRotation");

            Assert.That(firstAim, Is.EqualTo(secondAim).Using(QuaternionEqualityComparer.Instance));
            Assert.That(firstAim * Vector3.forward, Is.EqualTo(secondAim * Vector3.forward).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.AreNotEqual(Quaternion.Euler(5f, 35f, 0f), firstAim);
        }

        [Test]
        public void VisualRecoil_OnlyProducesBoundedCameraAndViewModelDeltas()
        {
            object state = Create("CGame.CameraVisualRecoilState");
            object request = CreateRequest(
                new Vector3(0.02f, -0.01f, -0.1f),
                new Vector3(-12f, 4f, 3f));

            Invoke(state, "Apply", request);
            object frame = Invoke(state, "Advance", 0f);
            object cameraDelta = GetProperty<object>(frame, "CameraDelta");
            object viewModelDelta = GetProperty<object>(frame, "ViewModelDelta");

            Assert.LessOrEqual(GetProperty<Vector3>(cameraDelta, "LocalPosition").magnitude, 0.08f);
            Assert.LessOrEqual(Quaternion.Angle(Quaternion.identity, GetProperty<Quaternion>(cameraDelta, "LocalRotation")), 5f);
            Assert.LessOrEqual(GetProperty<Vector3>(viewModelDelta, "LocalPosition").magnitude, 0.15f);
            Assert.LessOrEqual(Quaternion.Angle(Quaternion.identity, GetProperty<Quaternion>(viewModelDelta, "LocalRotation")), 8f);
            Assert.Greater(GetProperty<float>(cameraDelta, "Weight"), 0f);
            Assert.Greater(GetProperty<float>(viewModelDelta, "Weight"), 0f);

            Type stateType = state.GetType();
            Assert.IsFalse(typeof(Component).IsAssignableFrom(stateType));
            Assert.IsFalse(stateType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(field => field.FieldType == typeof(Transform)));
        }

        [Test]
        public void ClearingGameplayAndVisualStates_ReturnsBothChannelsToZero()
        {
            object gameplay = Create("CGame.GameplayRecoilState");
            object visual = Create("CGame.CameraVisualRecoilState");
            object request = CreateRequest(new Vector3(0f, 0f, -0.03f), new Vector3(-1f, 0.2f, 0f));
            Invoke(gameplay, "ApplyKick", GetProperty<Vector2>(request, "GameplayKick"), GetProperty<float>(request, "GameplayRecoveryDegreesPerSecond"));
            Invoke(visual, "Apply", request);

            Vector2 clearDelta = (Vector2)Invoke(gameplay, "Clear");
            Invoke(visual, "Reset");
            object frame = Invoke(visual, "Advance", 0f);

            Assert.AreNotEqual(Vector2.zero, clearDelta);
            Assert.That(GetProperty<Vector2>(gameplay, "Offset"), Is.EqualTo(Vector2.zero).Using(Vector2ComparerWithEqualsOperator.Instance));
            Assert.AreEqual(0f, GetProperty<float>(GetProperty<object>(frame, "CameraDelta"), "Weight"));
            Assert.AreEqual(0f, GetProperty<float>(GetProperty<object>(frame, "ViewModelDelta"), "Weight"));
        }

        [Test]
        public void GameplayRecoil_AtPitchLimitClearsOnlyTheAppliedKick()
        {
            object controller = Create("CGame.Controller");
            Invoke(controller, "SettingControlRotation", Quaternion.Euler(-88f, 20f, 0f));
            Quaternion startingRotation = GetProperty<Quaternion>(controller, "ControlRotation");

            Invoke(controller, "ApplyingGameplayRecoil", new Vector2(-5f, 0f), 9f);
            Assert.AreEqual(-89f, GetProperty<float>(controller, "ControlPitch"), 0.001f);
            Invoke(controller, "ClearingGameplayRecoil");

            Assert.That(
                GetProperty<Quaternion>(controller, "ControlRotation"),
                Is.EqualTo(startingRotation).Using(QuaternionEqualityComparer.Instance));
        }

        private static object CreateRequest(Vector3 cameraPosition, Vector3 cameraRotation)
        {
            return Create(
                "CGame.WeaponRecoilRequest",
                new Vector2(-1.2f, 0.25f),
                9f,
                cameraPosition,
                cameraRotation,
                new Vector3(0f, -0.015f, -0.06f),
                new Vector3(-3f, 0.4f, 0.2f),
                0.35f,
                18f);
        }

        private static void ApplyGameplayFromRequest(object controller, object request)
        {
            Invoke(
                controller,
                "ApplyingGameplayRecoil",
                GetProperty<Vector2>(request, "GameplayKick"),
                GetProperty<float>(request, "GameplayRecoveryDegreesPerSecond"));
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
