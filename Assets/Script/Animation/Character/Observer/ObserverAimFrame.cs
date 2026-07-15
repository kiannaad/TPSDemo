namespace CGame.Animation
{
    public readonly struct ObserverAimFrame
    {
        public ObserverAimFrame(
            float bodyYaw,
            float aimYaw,
            float aimPitch,
            ObserverWeaponState weaponState)
        {
            BodyYaw = bodyYaw;
            AimYaw = aimYaw;
            AimPitch = aimPitch;
            WeaponState = weaponState;
        }

        public float BodyYaw { get; }
        public float AimYaw { get; }
        public float AimPitch { get; }
        public ObserverWeaponState WeaponState { get; }
    }
}
