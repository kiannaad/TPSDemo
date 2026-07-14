using System;

namespace CGame
{
    public sealed class AIDecisionDebugSnapshot
    {
        private readonly AIUtilityCandidate[] candidates;
        private readonly AIAlertTransitionRecord[] history;

        public AIDecisionDebugSnapshot(
            double capturedAt,
            AIAlertState state,
            AIActionKind currentAction,
            AIActionStatus actionStatus,
            string selectionReason,
            float commitmentRemaining,
            AIUtilityCandidate[] candidates,
            AIAlertTransitionRecord[] history)
        {
            CapturedAt = capturedAt;
            State = state;
            CurrentAction = currentAction;
            ActionStatus = actionStatus;
            SelectionReason = selectionReason ?? string.Empty;
            CommitmentRemaining = Math.Max(0f, commitmentRemaining);
            this.candidates = candidates == null
                ? Array.Empty<AIUtilityCandidate>()
                : (AIUtilityCandidate[])candidates.Clone();
            this.history = history == null
                ? Array.Empty<AIAlertTransitionRecord>()
                : (AIAlertTransitionRecord[])history.Clone();
        }

        public double CapturedAt { get; }
        public AIAlertState State { get; }
        public AIActionKind CurrentAction { get; }
        public AIActionStatus ActionStatus { get; }
        public string SelectionReason { get; }
        public float CommitmentRemaining { get; }
        public AIUtilityCandidate[] Candidates => (AIUtilityCandidate[])candidates.Clone();
        public AIAlertTransitionRecord[] History => (AIAlertTransitionRecord[])history.Clone();
    }
}
