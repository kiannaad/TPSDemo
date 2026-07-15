using System;

namespace CGame
{
    public sealed class AIUtilitySelection
    {
        private readonly AIUtilityCandidate[] candidates;

        public AIUtilitySelection(
            AIActionKind selectedKind,
            string reason,
            AIUtilityCandidate[] candidates)
        {
            SelectedKind = selectedKind;
            Reason = reason ?? string.Empty;
            this.candidates = candidates == null
                ? Array.Empty<AIUtilityCandidate>()
                : (AIUtilityCandidate[])candidates.Clone();
        }

        public AIActionKind SelectedKind { get; }
        public string Reason { get; }
        public AIUtilityCandidate[] Candidates => (AIUtilityCandidate[])candidates.Clone();
    }
}
