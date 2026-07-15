using System;

namespace CGame
{
    public sealed class WeaponRuntime
    {
        private WeaponId equippedWeaponId;
        private uint generation;
        private ulong nextActionId;
        private WeaponActionFact activeAction;

        public event Action<WeaponEquipmentSnapshot> EquipmentChanged;
        public event Action<WeaponActionFact> ActionChanged;
        public event Action<WeaponActionFact> FireCommitted;

        public WeaponEquipmentSnapshot Snapshot => new WeaponEquipmentSnapshot(equippedWeaponId, generation);
        public WeaponActionFact ActiveAction => activeAction;

        public bool RequestEquip(WeaponId weaponId)
        {
            if (!weaponId.IsValid || equippedWeaponId == weaponId)
            {
                return false;
            }

            EndActiveAction(WeaponActionPhase.Cancelled, WeaponActionEndReason.EquipmentChanged);
            equippedWeaponId = weaponId;
            generation++;
            EquipmentChanged?.Invoke(Snapshot);
            return true;
        }

        public bool RequestUnequip()
        {
            if (!equippedWeaponId.IsValid)
            {
                return false;
            }

            EndActiveAction(WeaponActionPhase.Cancelled, WeaponActionEndReason.Unequipped);
            equippedWeaponId = default;
            generation++;
            EquipmentChanged?.Invoke(Snapshot);
            return true;
        }

        public bool RequestFire(out WeaponActionFact started, double authoritativeStartTime = 0d)
        {
            if (!equippedWeaponId.IsValid)
            {
                started = default;
                return false;
            }

            EndActiveAction(WeaponActionPhase.Cancelled, WeaponActionEndReason.Superseded);
            started = new WeaponActionFact(
                ++nextActionId,
                generation,
                equippedWeaponId,
                WeaponActionKind.Fire,
                WeaponActionPhase.Started,
                WeaponActionEndReason.None,
                authoritativeStartTime);
            activeAction = started;
            ActionChanged?.Invoke(started);
            FireCommitted?.Invoke(started);
            return true;
        }

        public bool CompleteAction(ulong actionId)
        {
            return activeAction.IsValid
                && activeAction.ActionId == actionId
                && EndActiveAction(WeaponActionPhase.Completed, WeaponActionEndReason.Completed);
        }

        public bool CancelAction(ulong actionId, WeaponActionEndReason reason = WeaponActionEndReason.Cancelled)
        {
            return activeAction.IsValid
                && activeAction.ActionId == actionId
                && EndActiveAction(WeaponActionPhase.Cancelled, reason);
        }

        public bool DisposeActiveAction()
        {
            return EndActiveAction(WeaponActionPhase.Cancelled, WeaponActionEndReason.OwnerDisposed);
        }

        private bool EndActiveAction(WeaponActionPhase phase, WeaponActionEndReason reason)
        {
            if (!activeAction.IsValid)
            {
                return false;
            }

            WeaponActionFact ended = activeAction.End(phase, reason);
            activeAction = default;
            ActionChanged?.Invoke(ended);
            return true;
        }
    }
}
