using System;

namespace CGame
{
    public sealed class AIPerceptionDebugSnapshot
    {
        private readonly AIPerceptionMemoryRecord[] records;

        public AIPerceptionDebugSnapshot(
            double capturedAt,
            int pendingStimulusCount,
            AIPerceptionMemoryRecord[] records)
        {
            CapturedAt = capturedAt;
            PendingStimulusCount = Math.Max(0, pendingStimulusCount);
            this.records = records == null
                ? Array.Empty<AIPerceptionMemoryRecord>()
                : (AIPerceptionMemoryRecord[])records.Clone();
        }

        public double CapturedAt { get; }
        public int PendingStimulusCount { get; }
        public AIPerceptionMemoryRecord[] Records => (AIPerceptionMemoryRecord[])records.Clone();
    }
}
