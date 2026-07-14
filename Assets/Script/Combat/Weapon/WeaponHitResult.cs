using UnityEngine;

namespace CGame
{
    public readonly struct WeaponHitResult
    {
        public WeaponHitResult(IDamageable target, Vector3 point, Vector3 normal)
        {
            Target = target;
            Point = point;
            Normal = normal;
        }

        public IDamageable Target { get; }
        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public bool HasTarget => Target != null;
    }
}
