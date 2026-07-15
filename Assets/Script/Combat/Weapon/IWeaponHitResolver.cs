using UnityEngine;

namespace CGame
{
    public interface IWeaponHitResolver
    {
        WeaponHitResult Resolve(Vector3 origin, Vector3 direction, float range, LayerMask hitMask);
    }
}
