namespace CGame
{
    public interface IAdsPresentationState
    {
        float AdsProgress { get; }
        bool IsAiming { get; }
        AimRejectionReason RejectionReason { get; }
    }
}
