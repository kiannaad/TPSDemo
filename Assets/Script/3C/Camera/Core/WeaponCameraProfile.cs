using UnityEngine;

namespace CGame
{
    [CreateAssetMenu(fileName = "WeaponCameraProfile", menuName = "CGame/Camera/Weapon Camera Profile")]
    public sealed class WeaponCameraProfile : ScriptableObject
    {
        [SerializeField]
        [Min(1f)]
        private float adsWorldFieldOfView = 48f;

        [SerializeField]
        [Min(1f)]
        private float adsViewModelFieldOfView = 62f;

        [SerializeField]
        [Min(0f)]
        private float adsEnterDuration = 0.18f;

        [SerializeField]
        [Min(0f)]
        private float adsExitDuration = 0.12f;

        [SerializeField]
        [Range(0f, 1f)]
        private float adsLookSensitivityMultiplier = 0.65f;

        [SerializeField]
        private Vector3 adsViewModelLocalPosition = new Vector3(-0.11f, 0.11f, 0f);

        public float AdsWorldFieldOfView => adsWorldFieldOfView;
        public float AdsViewModelFieldOfView => adsViewModelFieldOfView;
        public float AdsEnterDuration => adsEnterDuration;
        public float AdsExitDuration => adsExitDuration;
        public float AdsLookSensitivityMultiplier => adsLookSensitivityMultiplier;
        public Vector3 AdsViewModelLocalPosition => adsViewModelLocalPosition;
    }
}
