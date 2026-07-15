using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CGame.Tests
{
    public sealed class AIAlertDecisionContractTests
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
        public void AlertStateMachine_AllowsOnlyDeclaredTransitionsAndKeepsReentryStable()
        {
            object machine = Activator.CreateInstance(
                RequireRuntimeType("CGame.AIAlertStateMachine"),
                new object[] { 0d });
            Assert.AreEqual("Patrol", GetProperty<object>(machine, "State").ToString());

            Assert.IsTrue(Transition(machine, "Investigate", 1d, "sound"));
            Assert.IsTrue(Transition(machine, "Investigate", 1.1d, "same-state"));
            Assert.AreEqual(1d, GetProperty<double>(machine, "EnteredAt"));
            Assert.IsFalse(Transition(machine, "Patrol", 1.2d, "invalid-backtrack"));
            Assert.IsTrue(Transition(machine, "Combat", 2d, "visual"));
            Assert.IsTrue(Transition(machine, "Search", 3d, "lost-visual"));
            Assert.IsTrue(Transition(machine, "Return", 5d, "search-finished"));
            Assert.IsTrue(Transition(machine, "Patrol", 6d, "returned"));
        }

        [Test]
        public void UtilitySelection_IsRepeatableAndRespectsCooldown()
        {
            profile = CreateProfile();
            object context = CreateContext(
                "Combat",
                Vector3.zero,
                true,
                new Vector3(0f, 0f, 12f),
                0.9f,
                1f,
                10d);
            object firstSelector = Activator.CreateInstance(
                RequireRuntimeType("CGame.AIUtilitySelector"),
                new object[] { profile, 1337 });
            object secondSelector = Activator.CreateInstance(
                RequireRuntimeType("CGame.AIUtilitySelector"),
                new object[] { profile, 1337 });

            object first = Invoke(firstSelector, "Select", context);
            object second = Invoke(secondSelector, "Select", context);
            Assert.AreEqual(
                GetProperty<object>(first, "SelectedKind").ToString(),
                GetProperty<object>(second, "SelectedKind").ToString());
            Assert.AreEqual(
                CandidateScores(first),
                CandidateScores(second));

            object selectedKind = GetProperty<object>(first, "SelectedKind");
            Invoke(firstSelector, "SetCooldown", selectedKind, 20d);
            object afterCooldown = Invoke(firstSelector, "Select", context);
            Assert.AreNotEqual(
                selectedKind.ToString(),
                GetProperty<object>(afterCooldown, "SelectedKind").ToString());
        }

        [Test]
        public void ActionExecution_EnforcesCommitmentTimeoutAndIdempotentEndStates()
        {
            object request = CreateRequest("Hold", 0d, 1f, 3f);
            object execution = Activator.CreateInstance(
                RequireRuntimeType("CGame.AIActionExecution"),
                new[] { request });

            Assert.IsFalse((bool)Invoke(execution, "Cancel", 0.5d, "reevaluate", false));
            Assert.AreEqual("Running", GetProperty<object>(execution, "Status").ToString());
            Assert.IsTrue((bool)Invoke(execution, "Cancel", 0.5d, "target-death", true));
            Assert.AreEqual("Cancelled", GetProperty<object>(execution, "Status").ToString());
            Assert.IsTrue((bool)Invoke(execution, "Cancel", 0.6d, "repeat", true));

            object timed = Activator.CreateInstance(
                RequireRuntimeType("CGame.AIActionExecution"),
                new[] { CreateRequest("Aim", 2d, 0.5f, 1f) });
            object completed = Invoke(timed, "Advance", 3.01d);
            Assert.AreEqual("Completed", GetProperty<object>(completed, "Status").ToString());
        }

        [TestCase("Hold", "Completed")]
        [TestCase("Aim", "Failed")]
        [TestCase("Approach", "Cancelled")]
        [TestCase("Retreat", "Completed")]
        [TestCase("SearchPoint", "Failed")]
        public void ActionKinds_HaveExplicitCompletionFailureAndCancellation(
            string kind,
            string expectedStatus)
        {
            object execution = Activator.CreateInstance(
                RequireRuntimeType("CGame.AIActionExecution"),
                new[] { CreateRequest(kind, 0d, 0f, 5f) });
            object result;
            if (expectedStatus == "Failed")
            {
                result = Invoke(execution, "Fail", "test-failure");
            }
            else if (expectedStatus == "Cancelled")
            {
                Invoke(execution, "Cancel", 0d, "test-cancel", true);
                result = GetProperty<object>(execution, "Result");
            }
            else
            {
                result = Invoke(execution, "Complete", "test-complete");
            }

            Assert.AreEqual(expectedStatus, GetProperty<object>(result, "Status").ToString());
            Assert.AreEqual(Vector3.zero, GetProperty<Vector3>(result, "MovementDirection"));
        }

        [Test]
        public void DecisionDebugSnapshot_IsReadOnlyAndDoesNotAdvanceRuntimeFacts()
        {
            Type snapshotType = RequireRuntimeType("CGame.AIDecisionDebugSnapshot");
            Assert.IsFalse(snapshotType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(method => method.Name == "Advance"
                    || method.Name == "Select"
                    || method.Name == "TryTransition"
                    || method.Name.StartsWith("Submit", StringComparison.Ordinal)));
            Assert.IsFalse(snapshotType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(field => field.FieldType.FullName != null
                    && (field.FieldType.FullName.Contains("Transform")
                        || field.FieldType.FullName.Contains("Physics")
                        || field.FieldType.FullName.Contains("Random"))));
        }

        private ScriptableObject CreateProfile()
        {
            ScriptableObject created = ScriptableObject.CreateInstance(
                RequireRuntimeType("CGame.DecisionProfile"));
            SetField(created, "minimumCommitment", 0.5f);
            SetField(created, "actionCooldown", 1f);
            SetField(created, "preferredCombatDistance", 8f);
            SetField(created, "retreatDistance", 3f);
            SetField(created, "utilityJitter", 0.02f);
            return created;
        }

        private static object CreateContext(
            string state,
            Vector3 position,
            bool hasThreat,
            Vector3 threatPosition,
            float confidence,
            float health,
            double timestamp)
        {
            Type stateType = RequireRuntimeType("CGame.AIAlertState");
            return Activator.CreateInstance(
                RequireRuntimeType("CGame.AIDecisionContext"),
                Enum.Parse(stateType, state),
                position,
                hasThreat,
                threatPosition,
                confidence,
                health,
                timestamp);
        }

        private static object CreateRequest(
            string kind,
            double createdAt,
            float minimumCommitment,
            float maximumDuration)
        {
            Type kindType = RequireRuntimeType("CGame.AIActionKind");
            return Activator.CreateInstance(
                RequireRuntimeType("CGame.AIActionRequest"),
                Enum.Parse(kindType, kind),
                Vector3.zero,
                Vector3.forward,
                createdAt,
                minimumCommitment,
                maximumDuration);
        }

        private static bool Transition(object machine, string state, double timestamp, string reason)
        {
            Type stateType = RequireRuntimeType("CGame.AIAlertState");
            return (bool)Invoke(machine, "TryTransition", Enum.Parse(stateType, state), timestamp, reason);
        }

        private static string CandidateScores(object selection)
        {
            Array candidates = GetProperty<Array>(selection, "Candidates");
            return string.Join(
                "|",
                candidates.Cast<object>().Select(candidate =>
                    $"{GetProperty<object>(candidate, "Kind")}:{GetProperty<float>(candidate, "Score"):F5}"));
        }

        private static object Invoke(object target, string methodName, params object[] arguments)
        {
            return target.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single(method => method.Name == methodName && method.GetParameters().Length == arguments.Length)
                .Invoke(target, arguments);
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
