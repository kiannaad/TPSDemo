using System;

namespace CGame
{
    public readonly struct CoverCandidateScore
    {
        private readonly string[] reasons;

        public CoverCandidateScore(string slotId, float score, bool isViable, string[] reasons)
        {
            SlotId = slotId ?? string.Empty;
            Score = score;
            IsViable = isViable;
            this.reasons = reasons == null ? Array.Empty<string>() : (string[])reasons.Clone();
        }

        public string SlotId { get; }
        public float Score { get; }
        public bool IsViable { get; }
        public string[] Reasons => reasons == null ? Array.Empty<string>() : (string[])reasons.Clone();
    }
}
