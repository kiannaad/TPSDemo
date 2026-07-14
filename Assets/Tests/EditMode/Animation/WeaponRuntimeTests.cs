using System.Collections.Generic;
using CGame;
using NUnit.Framework;

namespace CGame.Tests
{
    public class WeaponRuntimeTests
    {
        [Test]
        public void EquipmentRequestsPublishOnlyChangedGenerations()
        {
            var runtime = new WeaponRuntime();
            var changes = new List<WeaponEquipmentSnapshot>();
            runtime.EquipmentChanged += changes.Add;

            Assert.IsTrue(runtime.RequestEquip(new WeaponId("rifle")));
            Assert.IsFalse(runtime.RequestEquip(new WeaponId("rifle")));
            Assert.IsTrue(runtime.RequestEquip(new WeaponId("pistol")));
            Assert.IsTrue(runtime.RequestUnequip());
            Assert.IsFalse(runtime.RequestUnequip());

            Assert.AreEqual(3, changes.Count);
            Assert.AreEqual(new WeaponId("rifle"), changes[0].EquippedWeaponId);
            Assert.AreEqual(1u, changes[0].Generation);
            Assert.AreEqual(new WeaponId("pistol"), changes[1].EquippedWeaponId);
            Assert.AreEqual(2u, changes[1].Generation);
            Assert.IsFalse(changes[2].IsEquipped);
            Assert.AreEqual(3u, changes[2].Generation);
            Assert.AreEqual(changes[2].Generation, runtime.Snapshot.Generation);
        }

        [Test]
        public void FireRequestsPublishUniqueStartedCommittedAndCompletedFacts()
        {
            var runtime = new WeaponRuntime();
            var facts = new List<WeaponActionFact>();
            var committed = new List<WeaponActionFact>();
            runtime.ActionChanged += facts.Add;
            runtime.FireCommitted += committed.Add;

            Assert.IsFalse(runtime.RequestFire(out _));
            Assert.IsTrue(runtime.RequestEquip(new WeaponId("rifle")));
            Assert.IsTrue(runtime.RequestFire(out WeaponActionFact first));
            Assert.AreEqual(WeaponActionPhase.Started, first.Phase);
            Assert.AreEqual(1u, first.Generation);
            Assert.IsTrue(runtime.CompleteAction(first.ActionId));
            Assert.IsFalse(runtime.CompleteAction(first.ActionId));
            Assert.IsTrue(runtime.RequestFire(out WeaponActionFact second));

            Assert.Greater(second.ActionId, first.ActionId);
            Assert.AreEqual(2, committed.Count);
            Assert.AreEqual(first.ActionId, committed[0].ActionId);
            Assert.AreEqual(WeaponActionPhase.Completed, facts[1].Phase);
            Assert.AreEqual(WeaponActionEndReason.Completed, facts[1].EndReason);
        }

        [Test]
        public void NewFireAndUnequipCancelOnlyCurrentActionWithExplicitReasons()
        {
            var runtime = new WeaponRuntime();
            var facts = new List<WeaponActionFact>();
            runtime.ActionChanged += facts.Add;
            runtime.RequestEquip(new WeaponId("rifle"));
            runtime.RequestFire(out WeaponActionFact first);
            runtime.RequestFire(out WeaponActionFact second);
            runtime.RequestUnequip();

            Assert.AreEqual(4, facts.Count);
            Assert.AreEqual(first.ActionId, facts[1].ActionId);
            Assert.AreEqual(WeaponActionEndReason.Superseded, facts[1].EndReason);
            Assert.AreEqual(second.ActionId, facts[3].ActionId);
            Assert.AreEqual(WeaponActionEndReason.Unequipped, facts[3].EndReason);
            Assert.IsFalse(runtime.ActiveAction.IsValid);
        }

        [Test]
        public void FireFactPreservesAuthoritativeStartTimeAcrossLifecycle()
        {
            var runtime = new WeaponRuntime();
            var facts = new List<WeaponActionFact>();
            runtime.ActionChanged += facts.Add;
            runtime.RequestEquip(new WeaponId("rifle"));

            Assert.IsTrue(runtime.RequestFire(out WeaponActionFact started, 1234.5d));
            Assert.AreEqual(1234.5d, started.AuthoritativeStartTime);
            Assert.IsTrue(runtime.CompleteAction(started.ActionId));
            Assert.AreEqual(1234.5d, facts[1].AuthoritativeStartTime);
        }
    }
}
