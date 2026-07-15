namespace CGame.Animation
{
    public readonly struct ObserverAimPresentationSnapshot
    {
        public ObserverAimPresentationSnapshot(
            bool isActive,
            float bodyYaw,
            float aimYawOffset,
            float aimPitch,
            float aimWeight,
            float adsWeight,
            float leftHandIkWeight,
            ObserverWeaponState weaponState,
            bool weaponVisible)
        {
            IsActive = isActive;
            BodyYaw = bodyYaw;
            AimYawOffset = aimYawOffset;
            AimPitch = aimPitch;
            AimWeight = aimWeight;
            AdsWeight = adsWeight;
            LeftHandIkWeight = leftHandIkWeight;
            WeaponState = weaponState;
            WeaponVisible = weaponVisible;
        }

        public bool IsActive { get; }
        public float BodyYaw { get; }
        public float AimYawOffset { get; }
        public float AimPitch { get; }
        public float AimWeight { get; }
        public float AdsWeight { get; }
        public float LeftHandIkWeight { get; }
        public ObserverWeaponState WeaponState { get; }
        public bool WeaponVisible { get; }
    }
}
