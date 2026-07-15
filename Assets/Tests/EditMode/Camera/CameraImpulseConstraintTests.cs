using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace CGame.Tests
{
    public sealed class CameraImpulseConstraintTests
    {
        [Test]
        public void RepeatedImpulse_IsDeterministicBoundedAndRecovers()
        {
            object first = Create("CGame.CameraImpulseState");
            object second = Create("CGame.CameraImpulseState");
            object request = CreateRequest(new Vector3(0.2f, -0.1f, -0.3f), new Vector3(12f, 8f, 5f));

            for (int index = 0; index < 3; index++)
            {
                Invoke(first, "Apply", request);
                Invoke(second, "Apply", request);
            }

            for (int index = 0; index < 240; index++)
            {
                object firstDelta = Invoke(first, "Advance", 1f / 60f);
                object secondDelta = Invoke(second, "Advance", 1f / 60f);
                Assert.That(
                    GetProperty<Vector3>(firstDelta, "LocalPosition"),
                    Is.EqualTo(GetProperty<Vector3>(secondDelta, "LocalPosition")).Using(Vector3ComparerWithEqualsOperator.Instance));
                Assert.That(
                    GetProperty<Quaternion>(firstDelta, "LocalRotation"),
                    Is.EqualTo(GetProperty<Quaternion>(secondDelta, "LocalRotation")).Using(QuaternionEqualityComparer.Instance));
                Assert.LessOrEqual(GetProperty<Vector3>(firstDelta, "LocalPosition").magnitude, 0.05f);
                Assert.LessOrEqual(Quaternion.Angle(Quaternion.identity, GetProperty<Quaternion>(firstDelta, "LocalRotation")), 3f);
            }

            Assert.AreEqual(0f, GetProperty<float>(Invoke(first, "Advance", 0f), "Weight"));
        }

        [Test]
        public void ImpulseState_DoesNotOwnOrModifyAimOrTransforms()
        {
            object controller = Create("CGame.Controller");
            Invoke(controller, "SettingControlRotation", Quaternion.Euler(10f, 25f, 0f));
            Quaternion aim = GetProperty<Quaternion>(controller, "ControlRotation");
            object state = Create("CGame.CameraImpulseState");

            Invoke(state, "Apply", CreateRequest(new Vector3(0.02f, 0f, -0.03f), new Vector3(-2f, 0.4f, 0f)));
            Invoke(state, "Advance", 0.016f);

            Assert.That(GetProperty<Quaternion>(controller, "ControlRotation"), Is.EqualTo(aim).Using(QuaternionEqualityComparer.Instance));
            Type stateType = state.GetType();
            Assert.IsFalse(typeof(Component).IsAssignableFrom(stateType));
            Assert.IsFalse(stateType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(field => field.FieldType == typeof(Transform)));
        }

        [Test]
        public void GeometryConstraint_CompressesOnlyLocalPosition()
        {
            Type constraintType = RequireRuntimeType("CGame.CameraImpulseCollisionConstraint");
            MethodInfo method = constraintType.GetMethod("CompressLocalPosition", BindingFlags.Static | BindingFlags.Public);
            Assert.NotNull(method);
            Vector3 requested = new Vector3(0f, 0f, -0.04f);

            Vector3 compressed = (Vector3)method.Invoke(null, new object[] { requested, 0.012f });

            Assert.AreEqual(0.012f, compressed.magnitude, 0.0001f);
            Assert.Greater(Vector3.Dot(requested.normalized, compressed.normalized), 0.999f);
        }

        [Test]
        public void Reset_ClearsPositionAndRotationImmediately()
        {
            object state = Create("CGame.CameraImpulseState");
            Invoke(state, "Apply", CreateRequest(new Vector3(0.02f, 0.01f, -0.03f), new Vector3(-2f, 1f, 0.5f)));

            Invoke(state, "Reset");
            object delta = Invoke(state, "Advance", 0f);

            Assert.AreEqual(Vector3.zero, GetProperty<Vector3>(delta, "LocalPosition"));
            Assert.AreEqual(Quaternion.identity, GetProperty<Quaternion>(delta, "LocalRotation"));
            Assert.AreEqual(0f, GetProperty<float>(delta, "Weight"));
        }

        private static object CreateRequest(Vector3 position, Vector3 rotation)
        {
            return Create("CGame.CameraImpulseRequest", position, rotation, 0.4f, 20f);
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
