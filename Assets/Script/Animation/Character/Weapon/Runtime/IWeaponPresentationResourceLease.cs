using System;

namespace CGame.Animation
{
    public interface IWeaponPresentationResourceLease : IDisposable
    {
        WeaponAnimationDefinition Definition { get; }
        string DefinitionId { get; }
        bool IsReleased { get; }
    }
}
