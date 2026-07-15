using UnityEngine;

namespace CGame
{
    public sealed class PhysicsWeaponHitResolver : IWeaponHitResolver
    {
        public WeaponHitResult Resolve(Vector3 origin, Vector3 direction, float range, LayerMask hitMask)
        {
            if (direction.sqrMagnitude <= 0.000001f
                || !Physics.Raycast(origin, direction.normalized, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                return default;
            }

            MonoBehaviour[] behaviours = hit.collider.GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IDamageable damageable)
                {
                    return new WeaponHitResult(damageable, hit.point, hit.normal);
                }
            }

            return new WeaponHitResult(null, hit.point, hit.normal);
        }
    }
}
