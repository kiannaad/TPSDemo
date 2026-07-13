namespace CGame.Animation
{
    public sealed class StopState : LocomotionStateBase
    {
        public StopState(IAnimationPlayableNode node)
            : base(LocomotionState.Stop, node)
        {
        }

        public float ElapsedTime { get; private set; }

        public override void Enter(AnimationGraphContext context)
        {
            ElapsedTime = 0f;
            base.Enter(context);
        }

        public override void Update(AnimationGraphContext context, float deltaTime)
        {
            ElapsedTime += deltaTime;
            base.Update(context, deltaTime);
        }
    }
}
