using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CGame.Tests
{
    public sealed class AISquadDeconflictionContractTests
    {
        [Test]
        public void Report_IsDelayedFuzzyExpiresAndNeverAuthorizesFire()
        {
            Type reportType = RequireRuntimeType("CGame.AISquadReport");
            object report = Activator.CreateInstance(
                reportType,
                "observer",
                "player",
                new Vector3(8f, 0f, 4f),
                10d,
                10.5d,
                14d,
                0.7f,
                2.5f);
            object context = Activator.CreateInstance(RequireRuntimeType("CGame.AISquadContext"));
            Assert.IsTrue((bool)Invoke(context, "PublishReport", report));

            object[] early = { "receiver", 10.49d, null };
            Assert.IsFalse((bool)InvokeWithArguments(context, "TryGetLatestSuggestion", early));
            object[] delivered = { "receiver", 10.5d, null };
            Assert.IsTrue((bool)InvokeWithArguments(context, "TryGetLatestSuggestion", delivered));
            Assert.IsFalse(GetProperty<bool>(delivered[2], "CanAuthorizeFire"));
            Assert.AreEqual(2.5f, GetProperty<float>(delivered[2], "UncertaintyRadius"));
            Invoke(context, "Advance", 14d);
            Assert.AreEqual(0, GetProperty<int>(context, "ReportCount"));
        }

        [Test]
        public void FourResourceLeases_CompeteExpireCancelAndReleaseByOwner()
        {
            object context = Activator.CreateInstance(RequireRuntimeType("CGame.AISquadContext"));
            Type kindType = RequireRuntimeType("CGame.AISquadResourceKind");
            foreach (string kindName in new[] { "Cover", "AttackAngle", "Shooter", "Reposition" })
            {
                object kind = Enum.Parse(kindType, kindName);
                object[] first = { kind, "shared", "ai-a", 10d, 1d, null };
                object[] competing = { kind, "shared", "ai-b", 10.5d, 1d, null };
                object[] afterExpiry = { kind, "shared", "ai-b", 11d, 1d, null };
                Assert.IsTrue((bool)InvokeWithArguments(context, "TryAcquire", first), kindName);
                Assert.IsFalse((bool)InvokeWithArguments(context, "TryAcquire", competing), kindName);
                Assert.IsTrue((bool)InvokeWithArguments(context, "TryAcquire", afterExpiry), kindName);
                Assert.IsTrue((bool)Invoke(afterExpiry[5], "Release"), kindName);
                Assert.IsFalse((bool)Invoke(afterExpiry[5], "Release"), kindName);
            }

            object shooter = Enum.Parse(kindType, "Shooter");
            object reposition = Enum.Parse(kindType, "Reposition");
            object[] shooterLease = { shooter, "slot", "departing", 20d, 5d, null };
            object[] repositionLease = { reposition, "slot", "departing", 20d, 5d, null };
            Assert.IsTrue((bool)InvokeWithArguments(context, "TryAcquire", shooterLease));
            Assert.IsTrue((bool)InvokeWithArguments(context, "TryAcquire", repositionLease));
            Assert.AreEqual(2, (int)Invoke(context, "ReleaseOwner", "departing"));
            Assert.AreEqual(0, GetProperty<int>(context, "LeaseCount"));
        }

        [Test]
        public void Context_OwnsOnlyReportsAndLeases_NotMemberTargetsStatesOrActions()
        {
            Type contextType = RequireRuntimeType("CGame.AISquadContext");
            string[] forbidden = { "target", "state", "action", "controller", "weapon" };
            string[] memberNames = contextType
                .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(member => member.Name.ToLowerInvariant())
                .ToArray();
            foreach (string name in forbidden)
            {
                Assert.IsFalse(memberNames.Any(member => member.Contains(name)), name);
            }
        }

        [Test]
        public void Context_ExercisesReportLeaseAndUpdateMarkersAtOneThreeAndSixMembers()
        {
            Type reportType = RequireRuntimeType("CGame.AISquadReport");
            Type kindType = RequireRuntimeType("CGame.AISquadResourceKind");
            object attackAngle = Enum.Parse(kindType, "AttackAngle");
            foreach (int memberCount in new[] { 1, 3, 6 })
            {
                object context = Activator.CreateInstance(RequireRuntimeType("CGame.AISquadContext"));
                for (int i = 0; i < memberCount; i++)
                {
                    object report = Activator.CreateInstance(
                        reportType,
                        $"ai-{i}",
                        "player",
                        new Vector3(i, 0f, 4f),
                        10d,
                        10.25d,
                        12d,
                        0.7f,
                        2f);
                    Assert.IsTrue((bool)Invoke(context, "PublishReport", report));
                    object[] leaseArguments =
                    {
                        attackAngle,
                        $"angle-{i}",
                        $"ai-{i}",
                        10d,
                        1d,
                        null,
                    };
                    Assert.IsTrue((bool)InvokeWithArguments(context, "TryAcquire", leaseArguments));
                }

                Invoke(context, "Advance", 10.5d);
                Assert.AreEqual(memberCount, GetProperty<int>(context, "ReportCount"));
                Assert.AreEqual(memberCount, GetProperty<int>(context, "LeaseCount"));
                Invoke(context, "Advance", 12d);
                Assert.AreEqual(0, GetProperty<int>(context, "ReportCount"));
                Assert.AreEqual(0, GetProperty<int>(context, "LeaseCount"));
            }
        }

        [Test]
        public void Suggestion_CanBeRejectedByIndividualAndNeverBecomesFirePermission()
        {
            object suggestion = Activator.CreateInstance(
                RequireRuntimeType("CGame.AISquadSuggestion"),
                "player",
                Vector3.one,
                10d,
                0.6f,
                3f);
            Assert.IsTrue((bool)Invoke(suggestion, "ShouldAccept", true, true, 0.5f));
            Assert.IsFalse((bool)Invoke(suggestion, "ShouldAccept", false, true, 0.5f));
            Assert.IsFalse((bool)Invoke(suggestion, "ShouldAccept", true, false, 0.5f));
            Assert.IsFalse((bool)Invoke(suggestion, "ShouldAccept", true, true, 0.8f));
            Assert.IsFalse(GetProperty<bool>(suggestion, "CanAuthorizeFire"));
        }

        private static object Invoke(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single(candidate => candidate.Name == methodName && candidate.GetParameters().Length == arguments.Length);
            return method.Invoke(target, arguments);
        }

        private static object InvokeWithArguments(object target, string methodName, object[] arguments)
        {
            return target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.Invoke(target, arguments);
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
