namespace CGame.Animation
{
    public interface IWeaponAnimationLayer : IAnimationPlayableNode
    {
        WeaponId WeaponId { get; }
        uint Generation { get; }
        bool IsPoseAvailable { get; }
        bool IsDisposed { get; }
        string SelectedLocomotionState { get; }
    }
}
