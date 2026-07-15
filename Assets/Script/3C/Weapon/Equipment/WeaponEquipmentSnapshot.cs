namespace CGame
{
    public readonly struct WeaponEquipmentSnapshot
    {
        public WeaponEquipmentSnapshot(WeaponId equippedWeaponId, uint generation)
        {
            EquippedWeaponId = equippedWeaponId;
            Generation = generation;
        }

        public WeaponId EquippedWeaponId { get; }
        public uint Generation { get; }
        public bool IsEquipped => EquippedWeaponId.IsValid;
    }
}
