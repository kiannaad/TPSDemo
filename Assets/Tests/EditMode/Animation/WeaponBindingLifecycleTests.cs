using System;
using System.Linq;
using CGame.Animation;
using NUnit.Framework;

namespace CGame.Tests
{
    public sealed class WeaponBindingLifecycleTests
    {
        [Test]
        public void Lifecycle_CoversBindingStatesAndDisposesIdempotently()
        {
            var lifecycle = new WeaponBindingLifecycle();
            Assert.AreEqual(WeaponBindingState.Unbound, lifecycle.State);

            WeaponPresentationLoadTicket first = lifecycle.Begin(new WeaponEquipmentSnapshot(new WeaponId("rifle"), 1u));
            Assert.AreEqual(WeaponBindingState.PendingPresentation, lifecycle.State);
            var activeLease = new FakeLease();
            Assert.IsTrue(lifecycle.Accept(first, activeLease, null, null, false));
            Assert.AreEqual(WeaponBindingState.Blending, lifecycle.State);
            lifecycle.CompleteBlend();
            Assert.AreEqual(WeaponBindingState.Active, lifecycle.State);

            WeaponPresentationLoadTicket degraded = lifecycle.Begin(new WeaponEquipmentSnapshot(new WeaponId("rifle"), 2u));
            Assert.IsTrue(lifecycle.Accept(degraded, new FakeLease(), null, null, true));
            lifecycle.CompleteBlend();
            Assert.AreEqual(WeaponBindingState.Degraded, lifecycle.State);

            WeaponPresentationLoadTicket fallback = lifecycle.Begin(new WeaponEquipmentSnapshot(new WeaponId("missing"), 3u));
            Assert.IsTrue(lifecycle.Reject(fallback));
            Assert.AreEqual(WeaponBindingState.Fallback, lifecycle.State);

            lifecycle.Begin(new WeaponEquipmentSnapshot(default, 4u));
            lifecycle.CompleteBlend();
            Assert.AreEqual(WeaponBindingState.Unbound, lifecycle.State);

            lifecycle.Dispose();
            lifecycle.Dispose();
            Assert.AreEqual(WeaponBindingState.Disposed, lifecycle.State);
            Assert.IsTrue(activeLease.IsReleased);
        }

        [Test]
        public void LoadTicket_RejectsStaleGenerationBindingAndCharacterTokens()
        {
            var lifecycle = new WeaponBindingLifecycle();
            WeaponPresentationLoadTicket first = lifecycle.Begin(new WeaponEquipmentSnapshot(new WeaponId("rifle"), 10u));
            WeaponPresentationLoadTicket latest = lifecycle.Begin(new WeaponEquipmentSnapshot(new WeaponId("rifle"), 11u));
            var staleLease = new FakeLease();

            Assert.IsFalse(lifecycle.Accept(first, staleLease, null, null, false));
            Assert.IsTrue(staleLease.IsReleased);
            Assert.IsTrue(lifecycle.CanAccept(latest));

            var otherCharacter = new WeaponBindingLifecycle();
            WeaponPresentationLoadTicket otherTicket = otherCharacter.Begin(new WeaponEquipmentSnapshot(new WeaponId("rifle"), 11u));
            var wrongCharacterLease = new FakeLease();
            Assert.IsFalse(lifecycle.Accept(otherTicket, wrongCharacterLease, null, null, false));
            Assert.IsTrue(wrongCharacterLease.IsReleased);

            lifecycle.Dispose();
            var afterDisposeLease = new FakeLease();
            Assert.IsFalse(lifecycle.Accept(latest, afterDisposeLease, null, null, false));
            Assert.IsTrue(afterDisposeLease.IsReleased);
            otherCharacter.Dispose();
        }

        [Test]
        public void RapidSwitch_ReleasesSupersededNextAndRetainsLatestOnly()
        {
            var lifecycle = new WeaponBindingLifecycle();
            WeaponPresentationLoadTicket firstTicket = lifecycle.Begin(new WeaponEquipmentSnapshot(new WeaponId("rifle-a"), 1u));
            var first = new FakeLease();
            lifecycle.Accept(firstTicket, first, null, null, false);
            lifecycle.CompleteBlend();

            WeaponPresentationLoadTicket secondTicket = lifecycle.Begin(new WeaponEquipmentSnapshot(new WeaponId("rifle-b"), 2u));
            var superseded = new FakeLease();
            lifecycle.Accept(secondTicket, superseded, null, null, false);
            WeaponPresentationLoadTicket latestTicket = lifecycle.Begin(new WeaponEquipmentSnapshot(new WeaponId("rifle-c"), 3u));
            Assert.IsTrue(superseded.IsReleased);
            Assert.IsFalse(first.IsReleased);

            var latest = new FakeLease();
            lifecycle.Accept(latestTicket, latest, null, null, false);
            lifecycle.CompleteBlend();
            Assert.IsTrue(first.IsReleased);
            Assert.IsFalse(latest.IsReleased);
            lifecycle.Dispose();
            Assert.IsTrue(latest.IsReleased);
        }

        [Test]
        public void ConsumerSnapshot_ExposesFactsWithoutPresentationDetails()
        {
            string[] properties = typeof(WeaponConsumerSnapshot)
                .GetProperties()
                .Select(property => property.Name)
                .OrderBy(name => name)
                .ToArray();
            CollectionAssert.AreEquivalent(new[]
            {
                "EquippedWeaponId",
                "Generation",
                "ActionId",
                "ActionType",
                "ActionPhase",
                "AuthoritativeStartTime",
                "AimState",
            }, properties);
            Assert.IsFalse(properties.Any(name =>
                name.Contains("Clip") || name.Contains("Slot") || name.Contains("Layer")
                || name.Contains("IK") || name.Contains("Notify") || name.Contains("Resource")));
        }

        private sealed class FakeLease : IWeaponPresentationResourceLease
        {
            public WeaponAnimationDefinition Definition => null;
            public string DefinitionId => "test-definition";
            public bool IsReleased { get; private set; }

            public void Dispose()
            {
                IsReleased = true;
            }
        }
    }
}
