namespace CGame.Animation
{
    public sealed class IdleState : LocomotionStateBase
    {
        public IdleState(IAnimationPlayableNode node)
            : base(LocomotionState.Idle, node)
        {
        }
    }
}
