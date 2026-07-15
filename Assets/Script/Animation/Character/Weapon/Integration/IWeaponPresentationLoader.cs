using System;

namespace CGame.Animation
{
    public interface IWeaponPresentationLoader
    {
        IDisposable BeginLoad(
            WeaponId weaponId,
            WeaponPresentationLoadTicket ticket,
            Action<WeaponPresentationLoadTicket, WeaponPresentationLoadResult> completed);
    }
}
