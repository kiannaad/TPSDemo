namespace CGame
{
    public readonly struct AIUtilityCandidate
    {
        public AIUtilityCandidate(AIActionKind kind, float score, string reason)
        {
            Kind = kind;
            Score = score;
            Reason = reason ?? string.Empty;
        }

        public AIActionKind Kind { get; }
        public float Score { get; }
        public string Reason { get; }
    }
}
