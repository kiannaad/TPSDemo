using UnityEngine;

namespace CGame.Animation
{
    public sealed class ObserverAimPresentationState
    {
        public const float MinAimPitch = -60f;
        public const float MaxAimPitch = 60f;
        public const float MaxAimYawOffset = 75f;

        private const float BodyTurnSpeed = 540f;
        private const float AimTurnSpeed = 360f;
        private const float WeightBlendSpeed = 8f;
        private float targetBodyYaw;
        private float targetAimYawOffset;
        private float targetAimPitch;
        private float bodyYaw;
        private float aimYawOffset;
        private float aimPitch;
        private float aimWeight;
        private float adsWeight;
        private float leftHandIkWeight;
        private ObserverWeaponState weaponState;
        private bool isActive;

        public ObserverAimPresentationSnapshot Snapshot => new ObserverAimPresentationSnapshot(
            isActive,
            bodyYaw,
            aimYawOffset,
            aimPitch,
            aimWeight,
            adsWeight,
            leftHandIkWeight,
            weaponState,
            isActive && weaponState != ObserverWeaponState.Holstered);

        public void Apply(ObserverAimFrame frame)
        {
            targetBodyYaw = Mathf.Repeat(frame.BodyYaw, 360f);
            targetAimYawOffset = Mathf.Clamp(
                Mathf.DeltaAngle(frame.BodyYaw, frame.AimYaw),
                -MaxAimYawOffset,
                MaxAimYawOffset);
            targetAimPitch = Mathf.Clamp(frame.AimPitch, MinAimPitch, MaxAimPitch);
            weaponState = frame.WeaponState;
            isActive = true;
        }

        public ObserverAimPresentationSnapshot Advance(float deltaTime)
        {
            float time = Mathf.Max(0f, deltaTime);
            bodyYaw = Mathf.MoveTowardsAngle(bodyYaw, targetBodyYaw, BodyTurnSpeed * time);
            aimYawOffset = Mathf.MoveTowards(aimYawOffset, targetAimYawOffset, AimTurnSpeed * time);
            aimPitch = Mathf.MoveTowards(aimPitch, targetAimPitch, AimTurnSpeed * time);
            float weaponTarget = isActive && weaponState != ObserverWeaponState.Holstered ? 1f : 0f;
            float adsTarget = isActive && weaponState == ObserverWeaponState.Ads ? 1f : 0f;
            float ikTarget = isActive && weaponState != ObserverWeaponState.Holstered ? 1f : 0f;
            aimWeight = Mathf.MoveTowards(aimWeight, weaponTarget, WeightBlendSpeed * time);
            adsWeight = Mathf.MoveTowards(adsWeight, adsTarget, WeightBlendSpeed * time);
            leftHandIkWeight = Mathf.MoveTowards(leftHandIkWeight, ikTarget, WeightBlendSpeed * time);
            return Snapshot;
        }

        public void Clear()
        {
            isActive = false;
            targetBodyYaw = bodyYaw;
            targetAimYawOffset = 0f;
            targetAimPitch = 0f;
            weaponState = ObserverWeaponState.Holstered;
        }
    }
}
