namespace CGame
{
    public readonly struct WeaponConsumerSnapshot
    {
        public WeaponConsumerSnapshot(
            WeaponId equippedWeaponId,
            uint generation,
            ulong actionId,
            WeaponActionKind actionType,
            WeaponActionPhase actionPhase,
            double authoritativeStartTime,
            WeaponAimState aimState)
        {
            EquippedWeaponId = equippedWeaponId;
            Generation = generation;
            ActionId = actionId;
            ActionType = actionType;
            ActionPhase = actionPhase;
            AuthoritativeStartTime = authoritativeStartTime;
            AimState = aimState;
        }

        public WeaponId EquippedWeaponId { get; }
        public uint Generation { get; }
        public ulong ActionId { get; }
        public WeaponActionKind ActionType { get; }
        public WeaponActionPhase ActionPhase { get; }
        public double AuthoritativeStartTime { get; }
        public WeaponAimState AimState { get; }
    }
}
