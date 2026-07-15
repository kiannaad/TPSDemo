namespace CGame
{
    public readonly struct WeaponActionFact
    {
        public WeaponActionFact(
            ulong actionId,
            uint generation,
            WeaponId weaponId,
            WeaponActionKind kind,
            WeaponActionPhase phase,
            WeaponActionEndReason endReason = WeaponActionEndReason.None,
            double authoritativeStartTime = 0d)
        {
            ActionId = actionId;
            Generation = generation;
            WeaponId = weaponId;
            Kind = kind;
            Phase = phase;
            EndReason = endReason;
            AuthoritativeStartTime = authoritativeStartTime;
        }

        public ulong ActionId { get; }
        public uint Generation { get; }
        public WeaponId WeaponId { get; }
        public WeaponActionKind Kind { get; }
        public WeaponActionPhase Phase { get; }
        public WeaponActionEndReason EndReason { get; }
        public double AuthoritativeStartTime { get; }
        public bool IsValid => ActionId > 0 && Generation > 0 && WeaponId.IsValid && Kind != WeaponActionKind.None;

        public WeaponActionFact End(WeaponActionPhase phase, WeaponActionEndReason reason)
        {
            return new WeaponActionFact(ActionId, Generation, WeaponId, Kind, phase, reason, AuthoritativeStartTime);
        }
    }
}
