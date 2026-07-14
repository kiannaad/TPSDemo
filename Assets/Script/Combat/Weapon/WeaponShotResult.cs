using UnityEngine;

namespace CGame
{
    public readonly struct WeaponShotResult
    {
        public WeaponShotResult(int sequence, Vector3 direction, WeaponHitResult hit, bool damageApplied)
        {
            Sequence = sequence;
            Direction = direction;
            Hit = hit;
            DamageApplied = damageApplied;
        }

        public int Sequence { get; }
        public Vector3 Direction { get; }
        public WeaponHitResult Hit { get; }
        public bool DamageApplied { get; }
    }
}
