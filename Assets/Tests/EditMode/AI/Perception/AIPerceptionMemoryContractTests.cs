using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CGame.Tests
{
    public sealed class AIPerceptionMemoryContractTests
    {
        private ScriptableObject profile;

        [TearDown]
        public void TearDown()
        {
            if (profile != null)
            {
                UnityEngine.Object.DestroyImmediate(profile);
                profile = null;
            }
        }

        [Test]
        public void VisualSensor_UsesRangeInclusiveFovOcclusionAndRecognitionTime()
        {
            profile = CreateProfile(
                viewDistance: 10f,
                fieldOfView: 90f,
                recognitionDuration: 0.5f);
            object sensor = Activator.CreateInstance(
                RequireRuntimeType("CGame.AIVisualSensor"),
                new object[] { profile });
            Vector3 boundaryPosition = Quaternion.Euler(0f, 45f, 0f) * Vector3.forward * 8f;

            object[] first = { "player", Vector3.zero, Vector3.forward, boundaryPosition, true, 10d, null };
            Assert.IsFalse((bool)InvokeWithArguments(sensor, "TryObserve", first));

            object[] recognized = { "player", Vector3.zero, Vector3.forward, boundaryPosition, true, 10.5d, null };
            Assert.IsTrue((bool)InvokeWithArguments(sensor, "TryObserve", recognized));
            Assert.AreEqual("Visual", GetProperty<object>(recognized[6], "Channel").ToString());
            Assert.IsTrue(GetProperty<bool>(recognized[6], "IsPrecise"));
            Assert.AreEqual(boundaryPosition, GetProperty<Vector3>(recognized[6], "Position"));

            Vector3 outsideFov = Quaternion.Euler(0f, 45.1f, 0f) * Vector3.forward * 8f;
            object[] outside = { "outside", Vector3.zero, Vector3.forward, outsideFov, true, 10d, null };
            Assert.IsFalse((bool)InvokeWithArguments(sensor, "TryObserve", outside));

            object[] blocked = { "blocked", Vector3.zero, Vector3.forward, Vector3.forward * 5f, false, 10d, null };
            Assert.IsFalse((bool)InvokeWithArguments(sensor, "TryObserve", blocked));

            object[] tooFar = { "far", Vector3.zero, Vector3.forward, Vector3.forward * 10.01f, true, 10d, null };
            Assert.IsFalse((bool)InvokeWithArguments(sensor, "TryObserve", tooFar));
        }

        [Test]
        public void Stimuli_PreserveSoundUncertaintyAndDamageDirectionWithOptionalSource()
        {
            Type stimulusType = RequireRuntimeType("CGame.AIStimulus");
            object sound = InvokeStatic(
                stimulusType,
                "CreateSound",
                "player",
                new Vector3(3f, 0f, 4f),
                12d,
                6f,
                0.55f);
            Assert.AreEqual("Sound", GetProperty<object>(sound, "Channel").ToString());
            Assert.IsFalse(GetProperty<bool>(sound, "IsPrecise"));
            Assert.AreEqual(6f, GetProperty<float>(sound, "UncertaintyRadius"));

            object damage = InvokeStatic(
                stimulusType,
                "CreateDamage",
                null,
                new Vector3(2f, 0f, 1f),
                new Vector3(3f, 0f, 0f),
                13d,
                0.8f);
            Assert.AreEqual("Damage", GetProperty<object>(damage, "Channel").ToString());
            Assert.AreEqual(string.Empty, GetProperty<string>(damage, "SourceEntityId"));
            Assert.Greater(Vector3.Dot(GetProperty<Vector3>(damage, "Direction"), Vector3.right), 0.999f);
        }

        [Test]
        public void Memory_DecaysExpiresAndNeverReadsMovedTargetPosition()
        {
            profile = CreateProfile(
                visualMemoryDuration: 4f,
                soundMemoryDuration: 2f,
                damageMemoryDuration: 3f);
            object memory = Activator.CreateInstance(
                RequireRuntimeType("CGame.AIPerceptionMemory"),
                new object[] { profile });
            Type stimulusType = RequireRuntimeType("CGame.AIStimulus");
            Vector3 observedPosition = new Vector3(1f, 0f, 2f);
            object visual = InvokeStatic(stimulusType, "CreateVisual", "player", observedPosition, 1d, 1f);

            Invoke(memory, "Observe", visual);
            Invoke(memory, "Advance", 3d);
            object[] lookup = { "player", null };
            Assert.IsTrue((bool)InvokeWithArguments(memory, "TryGet", lookup));
            object record = lookup[1];
            Assert.AreEqual(observedPosition, GetProperty<Vector3>(record, "LastKnownPosition"));
            Assert.AreEqual(0.5f, GetProperty<float>(record, "Confidence"), 0.001f);

            Vector3 movedTargetPosition = new Vector3(50f, 0f, 50f);
            Assert.AreNotEqual(movedTargetPosition, GetProperty<Vector3>(record, "LastKnownPosition"));

            Invoke(memory, "Advance", 5.01d);
            lookup = new object[] { "player", null };
            Assert.IsFalse((bool)InvokeWithArguments(memory, "TryGet", lookup));
        }

        [Test]
        public void DebugSnapshot_IsReadOnlyAndDoesNotOwnPhysicsOrStimulusConsumption()
        {
            Type snapshotType = RequireRuntimeType("CGame.AIPerceptionDebugSnapshot");
            string[] forbiddenFieldTypes = snapshotType
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(field => field.FieldType.FullName)
                .Where(name => name != null)
                .Where(name => name.Contains("Physics") || name.Contains("Queue") || name.Contains("Transform"))
                .ToArray();
            Assert.IsEmpty(forbiddenFieldTypes);
            Assert.IsFalse(snapshotType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(method => method.Name.Contains("Advance") || method.Name.Contains("Consume")));
        }

        [Test]
        public void PhysicsLineOfSight_UsesNearestNonObserverHitAcrossMultipleObstacles()
        {
            GameObject observer = new GameObject("PerceptionObserver");
            GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject nearObstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject farObstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                targetObject.transform.position = new Vector3(0f, 1.6f, 8f);
                nearObstacle.transform.position = new Vector3(0f, 1.6f, 2f);
                farObstacle.transform.position = new Vector3(0f, 1.6f, 5f);
                Component perceptionTarget = targetObject.AddComponent(
                    RequireRuntimeType("CGame.AIPerceptionTargetBehaviour"));
                Invoke(perceptionTarget, "Configure", "player", null);
                Physics.SyncTransforms();

                object query = Activator.CreateInstance(RequireRuntimeType("CGame.PhysicsAILineOfSightQuery"));
                Assert.IsFalse((bool)Invoke(
                    query,
                    "HasLineOfSight",
                    new Vector3(0f, 1.6f, 0f),
                    observer.transform,
                    perceptionTarget,
                    (LayerMask)~0));

                nearObstacle.SetActive(false);
                Physics.SyncTransforms();
                Assert.IsFalse((bool)Invoke(
                    query,
                    "HasLineOfSight",
                    new Vector3(0f, 1.6f, 0f),
                    observer.transform,
                    perceptionTarget,
                    (LayerMask)~0));

                farObstacle.SetActive(false);
                Physics.SyncTransforms();
                Assert.IsTrue((bool)Invoke(
                    query,
                    "HasLineOfSight",
                    new Vector3(0f, 1.6f, 0f),
                    observer.transform,
                    perceptionTarget,
                    (LayerMask)~0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(farObstacle);
                UnityEngine.Object.DestroyImmediate(nearObstacle);
                UnityEngine.Object.DestroyImmediate(targetObject);
                UnityEngine.Object.DestroyImmediate(observer);
            }
        }

        private ScriptableObject CreateProfile(
            float viewDistance = 20f,
            float fieldOfView = 100f,
            float recognitionDuration = 0.25f,
            float visualMemoryDuration = 5f,
            float soundMemoryDuration = 4f,
            float damageMemoryDuration = 6f)
        {
            ScriptableObject created = ScriptableObject.CreateInstance(RequireRuntimeType("CGame.PerceptionProfile"));
            SetField(created, "viewDistance", viewDistance);
            SetField(created, "horizontalFieldOfView", fieldOfView);
            SetField(created, "recognitionDuration", recognitionDuration);
            SetField(created, "visualMemoryDuration", visualMemoryDuration);
            SetField(created, "soundMemoryDuration", soundMemoryDuration);
            SetField(created, "damageMemoryDuration", damageMemoryDuration);
            return created;
        }

        private static object Invoke(object target, string methodName, params object[] arguments)
        {
            return target.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single(method => method.Name == methodName && method.GetParameters().Length == arguments.Length)
                .Invoke(target, arguments);
        }

        private static object InvokeWithArguments(object target, string methodName, object[] arguments)
        {
            return target.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single(method => method.Name == methodName && method.GetParameters().Length == arguments.Length)
                .Invoke(target, arguments);
        }

        private static object InvokeStatic(Type type, string methodName, params object[] arguments)
        {
            return type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Single(method => method.Name == methodName && method.GetParameters().Length == arguments.Length)
                .Invoke(null, arguments);
        }

        private static T GetProperty<T>(object target, string propertyName)
        {
            return (T)target.GetType()
                .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(target);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(target, value);
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
