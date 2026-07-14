using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;

namespace CGame.Tests
{
    public sealed class AIDebugAcceptanceContractTests
    {
        [Test]
        public void CompositeSnapshot_ExposesEveryRuntimeBoundaryAsReadOnlyProperties()
        {
            Type snapshotType = RequireRuntimeType("CGame.AICombatDebugSnapshot");
            foreach (string propertyName in new[]
            {
                "RuntimeId",
                "IsAlive",
                "Perception",
                "Navigation",
                "Decision",
                "CoverCombat",
                "Squad",
            })
            {
                PropertyInfo property = snapshotType.GetProperty(propertyName);
                Assert.NotNull(property, propertyName);
                Assert.IsFalse(property.CanWrite, propertyName);
            }
        }

        [Test]
        public void NavigationAndSquadSnapshots_DefensivelyCopyMutableCollections()
        {
            object navigation = Activator.CreateInstance(
                RequireRuntimeType("CGame.AINavigationDebugSnapshot"),
                true,
                Vector3.one,
                Enum.Parse(RequireRuntimeType("CGame.AINavigationPathStatus"), "Complete"),
                Enum.Parse(RequireRuntimeType("CGame.AIPathFollowState"), "Following"),
                0,
                new[] { Vector3.zero, Vector3.one });
            Vector3[] firstCorners = GetProperty<Vector3[]>(navigation, "Corners");
            firstCorners[0] = Vector3.up * 99f;
            Assert.AreEqual(Vector3.zero, GetProperty<Vector3[]>(navigation, "Corners")[0]);

            Type reportType = RequireRuntimeType("CGame.AISquadReport");
            object report = Activator.CreateInstance(
                reportType,
                "observer",
                "player",
                Vector3.one,
                1d,
                2d,
                3d,
                0.6f,
                2f);
            Array reports = Array.CreateInstance(reportType, 1);
            reports.SetValue(report, 0);
            Type leaseType = RequireRuntimeType("CGame.AISquadLeaseDebugRecord");
            object lease = Activator.CreateInstance(
                leaseType,
                Enum.Parse(RequireRuntimeType("CGame.AISquadResourceKind"), "Shooter"),
                "primary",
                "ai-a",
                3d);
            Array leases = Array.CreateInstance(leaseType, 1);
            leases.SetValue(lease, 0);
            object squad = Activator.CreateInstance(
                RequireRuntimeType("CGame.AISquadDebugSnapshot"),
                2d,
                "ai-a",
                reports,
                leases);
            Array firstReports = GetProperty<Array>(squad, "Reports");
            firstReports.SetValue(Activator.CreateInstance(
                reportType,
                "other",
                "player",
                Vector3.zero,
                1d,
                2d,
                3d,
                0.5f,
                1f), 0);
            Assert.AreEqual("observer", GetProperty<string>(GetProperty<Array>(squad, "Reports").GetValue(0), "ReporterId"));
        }

        [Test]
        public void DebugRuntime_HasNoUpdateLoopOrAuthoritativeMutationMethods()
        {
            Type debugType = RequireRuntimeType("CGame.AICombatDebugRuntimeBehaviour");
            string[] forbidden = { "Update", "Tick", "Advance", "SetDestination", "SubmitControlFrame", "PublishSound" };
            MethodInfo[] methods = debugType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (string name in forbidden)
            {
                Assert.IsFalse(methods.Any(method => method.Name == name), name);
            }

            Assert.NotNull(debugType.GetMethod("CreateDebugSnapshot"));
            Assert.NotNull(debugType.GetMethod("SetPanelVisible"));
        }

        [Test]
        public void PerceptionDecisionNavigationCoverAndSquad_DeclareProfilerMarkers()
        {
            foreach (string typeName in new[]
            {
                "CGame.AIPerceptionRuntimeBehaviour",
                "CGame.AIAlertDecisionRuntimeBehaviour",
                "CGame.AINavigationRuntimeBehaviour",
                "CGame.AICoverCombatRuntimeBehaviour",
                "CGame.AISquadContext",
            })
            {
                Type type = RequireRuntimeType(typeName);
                FieldInfo[] markers = type.GetFields(BindingFlags.Static | BindingFlags.NonPublic)
                    .Where(field => field.FieldType == typeof(ProfilerMarker))
                    .ToArray();
                Assert.IsNotEmpty(markers, typeName);
            }
        }

        private static T GetProperty<T>(object target, string propertyName)
        {
            return (T)target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
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
