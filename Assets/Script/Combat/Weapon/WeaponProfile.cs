using UnityEngine;

namespace CGame
{
    [CreateAssetMenu(fileName = "WeaponProfile", menuName = "CGame/Combat/Weapon Profile")]
    public sealed class WeaponProfile : ScriptableObject
    {
        [SerializeField]
        [Min(1)]
        private int magazineCapacity = 30;

        [SerializeField]
        [Min(0.001f)]
        private float secondsPerShot = 0.1f;

        [SerializeField]
        [Min(0f)]
        private float reloadDuration = 1.5f;

        [SerializeField]
        [Min(0.01f)]
        private float damage = 25f;

        [SerializeField]
        [Min(0.01f)]
        private float range = 100f;

        [SerializeField]
        [Range(0f, 45f)]
        private float spreadDegrees;

        [SerializeField]
        private LayerMask hitMask = ~0;

        public int MagazineCapacity => magazineCapacity;
        public float SecondsPerShot => secondsPerShot;
        public float ReloadDuration => reloadDuration;
        public float Damage => damage;
        public float Range => range;
        public float SpreadDegrees => spreadDegrees;
        public LayerMask HitMask => hitMask;

        public bool IsValid => magazineCapacity > 0
            && secondsPerShot > 0f
            && reloadDuration >= 0f
            && damage > 0f
            && range > 0f
            && spreadDegrees >= 0f
            && spreadDegrees <= 45f;
    }
}
