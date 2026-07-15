using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

namespace CGame.Tests
{
    public sealed class AINavigationContractTests
    {
        private NavMeshDataInstance navMeshInstance;
        private NavMeshData navMeshData;

        [TearDown]
        public void TearDown()
        {
            navMeshInstance.Remove();
            if (navMeshData != null)
            {
                UnityEngine.Object.DestroyImmediate(navMeshData);
                navMeshData = null;
            }
        }

        [Test]
        public void NavigationContracts_DoNotOwnCharacterPosition()
        {
            Type queryType = RequireRuntimeType("CGame.IAINavigationQuery");
            Type followerType = RequireRuntimeType("CGame.AIPathFollower");
            Type runtimeType = RequireRuntimeType("CGame.AINavigationRuntimeBehaviour");

            Assert.IsTrue(queryType.IsInterface);
            Assert.IsTrue(typeof(MonoBehaviour).IsAssignableFrom(runtimeType));
            string[] forbiddenFollowerTypes = followerType
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(field => field.FieldType.FullName)
                .Where(name => name != null)
                .Where(name => name.Contains("Transform") || name.Contains("CharacterPhysicsMotor") || name.Contains("NavMeshAgent"))
                .ToArray();
            Assert.IsEmpty(forbiddenFollowerTypes);
        }

        [Test]
        public void PathFollower_AdvancesCornersArrivesAndCancelsIdempotently()
        {
            object follower = CreateFollower();
            object path = CreatePathResult("Complete", new[]
            {
                Vector3.zero,
                new Vector3(2f, 0f, 0f),
                new Vector3(2f, 0f, 2f),
            });

            object output = Invoke(follower, "SetPath", path, Vector3.zero);
            Assert.AreEqual("Following", GetProperty<object>(output, "State").ToString());
            AssertDirection(GetProperty<Vector3>(output, "MovementDirection"), Vector3.right);

            output = Invoke(follower, "Advance", new Vector3(2f, 0f, 0f), 0.1f);
            AssertDirection(GetProperty<Vector3>(output, "MovementDirection"), Vector3.forward);

            output = Invoke(follower, "Advance", new Vector3(2f, 0f, 2f), 0.1f);
            Assert.AreEqual("Arrived", GetProperty<object>(output, "State").ToString());
            Assert.AreEqual(Vector3.zero, GetProperty<Vector3>(output, "MovementDirection"));

            object cancelled = Invoke(follower, "Cancel");
            object cancelledAgain = Invoke(follower, "Cancel");
            Assert.AreEqual("Cancelled", GetProperty<object>(cancelled, "State").ToString());
            Assert.AreEqual("Cancelled", GetProperty<object>(cancelledAgain, "State").ToString());
        }

        [Test]
        public void PathFollower_ReportsStuckExpiryAndPartialPathRequery()
        {
            object stuckFollower = CreateFollower(0.2f, 0.5f, 0.1f, 10f);
            object completePath = CreatePathResult("Complete", new[] { Vector3.zero, new Vector3(4f, 0f, 0f) });
            Invoke(stuckFollower, "SetPath", completePath, Vector3.zero);
            Invoke(stuckFollower, "Advance", Vector3.zero, 0.3f);
            object stuck = Invoke(stuckFollower, "Advance", Vector3.zero, 0.3f);
            Assert.AreEqual("Stuck", GetProperty<object>(stuck, "State").ToString());

            object expiringFollower = CreateFollower(0.2f, 5f, 0.1f, 0.5f);
            Invoke(expiringFollower, "SetPath", completePath, Vector3.zero);
            object expired = Invoke(expiringFollower, "Advance", Vector3.zero, 0.6f);
            Assert.AreEqual("NeedsRepath", GetProperty<object>(expired, "State").ToString());

            object partialFollower = CreateFollower();
            object partialPath = CreatePathResult("Partial", new[] { Vector3.zero, Vector3.right });
            Invoke(partialFollower, "SetPath", partialPath, Vector3.zero);
            object partialEnd = Invoke(partialFollower, "Advance", Vector3.right, 0.1f);
            Assert.AreEqual("NeedsRepath", GetProperty<object>(partialEnd, "State").ToString());
        }

        [Test]
        public void NavMeshQuery_ReportsCompleteOutsideEndpointsAndCancellation()
        {
            BuildObstacleNavMesh();
            Type queryType = RequireRuntimeType("CGame.NavMeshNavigationQuery");
            object query = Activator.CreateInstance(queryType, 0.5f, NavMesh.AllAreas);

            object complete = Invoke(query, "CalculatePath", new Vector3(-4f, 0f, -4f), new Vector3(4f, 0f, 4f));
            Assert.AreEqual("Complete", GetProperty<object>(complete, "Status").ToString());
            Assert.GreaterOrEqual(GetProperty<Vector3[]>(complete, "Corners").Length, 3);

            object outsideStart = Invoke(query, "CalculatePath", new Vector3(-30f, 0f, -30f), Vector3.zero);
            Assert.AreEqual("StartOutsideNavMesh", GetProperty<object>(outsideStart, "Status").ToString());

            object outsideTarget = Invoke(query, "CalculatePath", new Vector3(-4f, 0f, -4f), new Vector3(30f, 0f, 30f));
            Assert.AreEqual("DestinationOutsideNavMesh", GetProperty<object>(outsideTarget, "Status").ToString());

            object cancelled = Invoke(query, "Cancel");
            Assert.AreEqual("Cancelled", GetProperty<object>(cancelled, "Status").ToString());
        }

        private void BuildObstacleNavMesh()
        {
            NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(0);
            var sources = new List<NavMeshBuildSource>
            {
                CreateBoxSource(new Vector3(0f, -0.1f, 0f), new Vector3(12f, 0.2f, 12f), 0),
                CreateBoxSource(new Vector3(0f, 1f, 0f), new Vector3(2f, 2f, 8f), 1),
            };
            navMeshData = NavMeshBuilder.BuildNavMeshData(
                settings,
                sources,
                new Bounds(Vector3.zero, new Vector3(14f, 6f, 14f)),
                Vector3.zero,
                Quaternion.identity);
            Assert.NotNull(navMeshData);
            navMeshInstance = NavMesh.AddNavMeshData(navMeshData);
        }

        private static NavMeshBuildSource CreateBoxSource(Vector3 position, Vector3 size, int area)
        {
            return new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                transform = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one),
                size = size,
                area = area,
            };
        }

        private static object CreateFollower(
            float cornerTolerance = 0.2f,
            float progressTimeout = 1f,
            float minimumProgress = 0.05f,
            float maxPathAge = 3f)
        {
            return Activator.CreateInstance(
                RequireRuntimeType("CGame.AIPathFollower"),
                cornerTolerance,
                progressTimeout,
                minimumProgress,
                maxPathAge);
        }

        private static object CreatePathResult(string statusName, Vector3[] corners)
        {
            Type statusType = RequireRuntimeType("CGame.AINavigationPathStatus");
            return Activator.CreateInstance(
                RequireRuntimeType("CGame.AINavigationPathResult"),
                Enum.Parse(statusType, statusName),
                corners);
        }

        private static object Invoke(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single(candidate => candidate.Name == methodName && candidate.GetParameters().Length == arguments.Length);
            return method.Invoke(target, arguments);
        }

        private static T GetProperty<T>(object target, string propertyName)
        {
            return (T)target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(target);
        }

        private static void AssertDirection(Vector3 actual, Vector3 expected)
        {
            Assert.Greater(Vector3.Dot(actual.normalized, expected.normalized), 0.999f);
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
