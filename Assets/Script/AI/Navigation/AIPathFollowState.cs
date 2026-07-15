namespace CGame
{
    public enum AIPathFollowState
    {
        Idle,
        Following,
        Arrived,
        NeedsRepath,
        Stuck,
        Failed,
        Cancelled,
    }
}
