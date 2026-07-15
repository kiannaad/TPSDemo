using UnityEngine;

namespace CGame.Animation
{
    [CreateAssetMenu(menuName = "CGame/Animation/Weapon Animation Definition", fileName = "WeaponAnimationDefinition")]
    public sealed class WeaponAnimationDefinition : ScriptableObject
    {
        [SerializeField] private string weaponId;
        [SerializeField] private AnimationClipAsset idle;
        [SerializeField] private AnimationClipAsset walk;
        [SerializeField] private AnimationClipAsset run;
        [SerializeField] private AnimationClipAsset stop;
        [SerializeField] private AnimationClipAsset fire;
        [SerializeField] private AnimationClip weaponModelFire;
        [SerializeField] private GameObject presentationPrefab;
        [SerializeField, Min(0f)] private float blendDuration = 0.15f;
        [SerializeField, Range(0f, 90f)] private float aimYawRange = 75f;
        [SerializeField, Range(0f, 90f)] private float aimPitchUpRange = 60f;
        [SerializeField, Range(0f, 90f)] private float aimPitchDownRange = 45f;
        [SerializeField, Range(0f, 1f)] private float aimWeight = 0.75f;
        [SerializeField, Min(0f)] private float aimSmoothingTime = 0.08f;
        [SerializeField, Min(0f)] private float leftHandIkSmoothingTime = 0.08f;
        [SerializeField, Min(0f)] private float recoilImpulse = 4f;
        [SerializeField, Min(0f)] private float recoilMaxPitch = 9f;
        [SerializeField, Min(0.001f)] private float recoilDecayTime = 0.12f;

        public WeaponId WeaponId => new WeaponId(weaponId);
        public AnimationClipAsset Idle => idle;
        public AnimationClipAsset Walk => walk;
        public AnimationClipAsset Run => run;
        public AnimationClipAsset Stop => stop;
        public AnimationClipAsset Fire => fire;
        public AnimationClip WeaponModelFire => weaponModelFire;
        public GameObject PresentationPrefab => presentationPrefab;
        public float BlendDuration => Mathf.Max(0f, blendDuration);
        public float AimYawRange => Mathf.Max(0f, aimYawRange);
        public float AimPitchUpRange => Mathf.Max(0f, aimPitchUpRange);
        public float AimPitchDownRange => Mathf.Max(0f, aimPitchDownRange);
        public float AimWeight => Mathf.Clamp01(aimWeight);
        public float AimSmoothingTime => Mathf.Max(0f, aimSmoothingTime);
        public float LeftHandIkSmoothingTime => Mathf.Max(0f, leftHandIkSmoothingTime);
        public float RecoilImpulse => Mathf.Max(0f, recoilImpulse);
        public float RecoilMaxPitch => Mathf.Max(0f, recoilMaxPitch);
        public float RecoilDecayTime => Mathf.Max(0.001f, recoilDecayTime);
        public bool IsValid => WeaponId.IsValid
            && IsValidAsset(idle)
            && (IsValidAsset(walk) || IsValidAsset(run))
            && IsValidAsset(fire)
            && weaponModelFire != null;

        public bool HasPoseFor(string locomotionState)
        {
            switch (locomotionState)
            {
                case "Idle":
                    return IsValidAsset(idle);
                case "Move":
                    return IsValidAsset(walk) || IsValidAsset(run);
                case "Stop":
                    return IsValidAsset(stop);
                default:
                    return false;
            }
        }

        private static bool IsValidAsset(AnimationClipAsset asset)
        {
            return asset != null && asset.IsValid;
        }
    }
}
