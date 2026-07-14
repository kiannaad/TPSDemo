using System;

namespace CGame
{
    public sealed class AICoverCombatDebugSnapshot
    {
        private readonly CoverCandidateScore[] candidates;

        public AICoverCombatDebugSnapshot(
            double capturedAt,
            AICombatActionState action,
            CoverStance stance,
            string reservedSlotId,
            float aimProgress,
            int burstShotsRemaining,
            string reason,
            CoverCandidateScore[] candidates)
        {
            CapturedAt = capturedAt;
            Action = action;
            Stance = stance;
            ReservedSlotId = reservedSlotId ?? string.Empty;
            AimProgress = Math.Max(0f, Math.Min(1f, aimProgress));
            BurstShotsRemaining = Math.Max(0, burstShotsRemaining);
            Reason = reason ?? string.Empty;
            this.candidates = candidates == null
                ? Array.Empty<CoverCandidateScore>()
                : (CoverCandidateScore[])candidates.Clone();
        }

        public double CapturedAt { get; }
        public AICombatActionState Action { get; }
        public CoverStance Stance { get; }
        public string ReservedSlotId { get; }
        public float AimProgress { get; }
        public int BurstShotsRemaining { get; }
        public string Reason { get; }
        public CoverCandidateScore[] Candidates => (CoverCandidateScore[])candidates.Clone();
    }
}
