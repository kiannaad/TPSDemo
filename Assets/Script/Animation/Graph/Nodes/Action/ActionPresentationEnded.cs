namespace CGame.Animation
{
    public readonly struct ActionPresentationEnded
    {
        public ActionPresentationEnded(ulong requestId, ActionPresentationEndReason reason)
        {
            RequestId = requestId;
            Reason = reason;
        }

        public ulong RequestId { get; }
        public ActionPresentationEndReason Reason { get; }
    }
}
