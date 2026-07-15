using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CGame.Tests
{
    public sealed class AICoverCombatContractTests
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
        public void CoverScoring_IsExplainableAndRejectsInvalidOrOccupiedSlots()
        {
            profile = ScriptableObject.CreateInstance(RequireRuntimeType("CGame.CombatProfile"));
            object scorer = Activator.CreateInstance(
                RequireRuntimeType("CGame.CoverScorer"),
                new object[] { profile });
            Type contextType = RequireRuntimeType("CGame.CoverEvaluationContext");
            object safeContext = Activator.CreateInstance(
                contextType,
                true,
                true,
                true,
                6f,
                8f,
                0.1f,
                0.1f,
                Enum.Parse(RequireRuntimeType("CGame.CoverStance"), "Standing"),
                false);
            object riskyContext = Activator.CreateInstance(
                contextType,
                true,
                true,
                true,
                6f,
                8f,
                0.9f,
                0.9f,
                Enum.Parse(RequireRuntimeType("CGame.CoverStance"), "Crouching"),
                false);
            object occupiedContext = Activator.CreateInstance(
                contextType,
                true,
                true,
                true,
                6f,
                8f,
                0f,
                0f,
                Enum.Parse(RequireRuntimeType("CGame.CoverStance"), "Standing"),
                true);

            object safe = Invoke(scorer, "Evaluate", "safe", safeContext);
            object risky = Invoke(scorer, "Evaluate", "risky", riskyContext);
            object occupied = Invoke(scorer, "Evaluate", "occupied", occupiedContext);

            Assert.IsTrue(GetProperty<bool>(safe, "IsViable"));
            Assert.Greater(GetProperty<float>(safe, "Score"), GetProperty<float>(risky, "Score"));
            Assert.IsFalse(GetProperty<bool>(occupied, "IsViable"));
            CollectionAssert.Contains(GetProperty<string[]>(occupied, "Reasons"), "occupied");
            Assert.GreaterOrEqual(GetProperty<string[]>(safe, "Reasons").Length, 5);
        }

        [Test]
        public void CoverScoring_RequiresReachabilityOcclusionAndLineOfFire()
        {
            profile = ScriptableObject.CreateInstance(RequireRuntimeType("CGame.CombatProfile"));
            object scorer = Activator.CreateInstance(
                RequireRuntimeType("CGame.CoverScorer"),
                new object[] { profile });
            Type contextType = RequireRuntimeType("CGame.CoverEvaluationContext");
            string[] expectedReason = { "unreachable", "no-occlusion", "no-line-of-fire" };
            for (int index = 0; index < expectedReason.Length; index++)
            {
                object context = Activator.CreateInstance(
                    contextType,
                    index != 0,
                    index != 1,
                    index != 2,
                    5f,
                    8f,
                    0f,
                    0f,
                    Enum.Parse(RequireRuntimeType("CGame.CoverStance"), "Standing"),
                    false);
                object score = Invoke(scorer, "Evaluate", $"invalid-{index}", context);
                Assert.IsFalse(GetProperty<bool>(score, "IsViable"));
                CollectionAssert.Contains(GetProperty<string[]>(score, "Reasons"), expectedReason[index]);
            }
        }

        [Test]
        public void CoverReservation_IsExclusiveOwnerCheckedAndIdempotent()
        {
            object service = Activator.CreateInstance(RequireRuntimeType("CGame.CoverReservationService"));
            object[] firstArguments = { "slot-a", "ai-a", null };
            object[] competingArguments = { "slot-a", "ai-b", null };
            Assert.IsTrue((bool)InvokeWithArguments(service, "TryReserve", firstArguments));
            Assert.IsFalse((bool)InvokeWithArguments(service, "TryReserve", competingArguments));
            object reservation = firstArguments[2];
            Assert.NotNull(reservation);
            Assert.IsTrue(GetProperty<bool>(reservation, "IsActive"));
            Assert.IsFalse((bool)Invoke(service, "Release", "slot-a", "ai-b"));
            Assert.IsTrue((bool)Invoke(reservation, "Release"));
            Assert.IsFalse((bool)Invoke(reservation, "Release"));
            Assert.IsFalse(GetProperty<bool>(reservation, "IsActive"));
            Assert.IsTrue((bool)InvokeWithArguments(service, "TryReserve", competingArguments));
            Assert.IsTrue((bool)Invoke(competingArguments[2], "Release"));
        }

        [Test]
        public void CombatProfile_DrivesAimBurstDistanceAndPressureError()
        {
            profile = ScriptableObject.CreateInstance(RequireRuntimeType("CGame.CombatProfile"));
            Assert.IsTrue(GetProperty<bool>(profile, "IsValid"));
            Assert.Greater(GetProperty<float>(profile, "AimConvergenceDuration"), 0f);
            Assert.Greater(GetProperty<int>(profile, "BurstLength"), 0);
            Assert.Greater(GetProperty<float>(profile, "BurstInterval"), 0f);
            Assert.Greater(GetProperty<float>(profile, "PreferredDistance"), 0f);

            Type modelType = RequireRuntimeType("CGame.AICombatAimModel");
            float settled = (float)InvokeStatic(modelType, "CalculateErrorDegrees", profile, 1f, false, 0f);
            float moving = (float)InvokeStatic(modelType, "CalculateErrorDegrees", profile, 1f, true, 0f);
            float pressured = (float)InvokeStatic(modelType, "CalculateErrorDegrees", profile, 1f, false, 1f);
            float unconverged = (float)InvokeStatic(modelType, "CalculateErrorDegrees", profile, 0f, false, 0f);
            Assert.Greater(moving, settled);
            Assert.Greater(pressured, settled);
            Assert.Greater(unconverged, settled);
        }

        [Test]
        public void CombatActionAndDebugContracts_ExposeEveryRequiredStateWithoutMutableArrays()
        {
            Type actionType = RequireRuntimeType("CGame.AICombatActionState");
            string[] required =
            {
                "MoveToCover",
                "Aim",
                "FireBurst",
                "Reposition",
                "Approach",
                "Retreat",
            };
            foreach (string name in required)
            {
                Assert.IsTrue(Enum.IsDefined(actionType, name), $"Missing combat action: {name}");
            }

            Type scoreType = RequireRuntimeType("CGame.CoverCandidateScore");
            object score = Activator.CreateInstance(
                scoreType,
                "slot-a",
                1f,
                true,
                new[] { "reachable", "occluded" });
            Array scores = Array.CreateInstance(scoreType, 1);
            scores.SetValue(score, 0);
            object snapshot = Activator.CreateInstance(
                RequireRuntimeType("CGame.AICoverCombatDebugSnapshot"),
                10d,
                Enum.Parse(actionType, "Aim"),
                Enum.Parse(RequireRuntimeType("CGame.CoverStance"), "Standing"),
                "slot-a",
                0.5f,
                2,
                "test",
                scores);
            Array first = GetProperty<Array>(snapshot, "Candidates");
            first.SetValue(Activator.CreateInstance(
                scoreType,
                "mutated",
                0f,
                false,
                Array.Empty<string>()), 0);
            Array second = GetProperty<Array>(snapshot, "Candidates");
            Assert.AreEqual("slot-a", GetProperty<string>(second.GetValue(0), "SlotId"));
        }

        private static object Invoke(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single(candidate => candidate.Name == methodName
                    && candidate.GetParameters().Length == arguments.Length);
            return method.Invoke(target, arguments);
        }

        private static object InvokeWithArguments(object target, string methodName, object[] arguments)
        {
            return target.GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.Invoke(target, arguments);
        }

        private static object InvokeStatic(Type type, string methodName, params object[] arguments)
        {
            return type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                ?.Invoke(null, arguments);
        }

        private static T GetProperty<T>(object target, string propertyName)
        {
            return (T)target.GetType()
                .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
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
