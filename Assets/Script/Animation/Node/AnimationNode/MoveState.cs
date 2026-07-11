namespace CGame.Animation
{
    public sealed class MoveState : LocomotionStateBase
    {
        public MoveState(IAnimationPlayableNode node)
            : base(LocomotionState.Move, node)
        {
        }
    }
}
