using UnityEngine;

namespace CGame
{
    public readonly struct AIPerceptionMemoryRecord
    {
        public AIPerceptionMemoryRecord(
            string memoryKey,
            string sourceEntityId,
            AIPerceptionChannel channel,
            Vector3 lastKnownPosition,
            Vector3 direction,
            double observedAt,
            double expiresAt,
            float uncertaintyRadius,
            float initialConfidence,
            float confidence,
            bool isPrecise)
        {
            MemoryKey = memoryKey;
            SourceEntityId = sourceEntityId;
            Channel = channel;
            LastKnownPosition = lastKnownPosition;
            Direction = direction;
            ObservedAt = observedAt;
            ExpiresAt = expiresAt;
            UncertaintyRadius = uncertaintyRadius;
            InitialConfidence = initialConfidence;
            Confidence = confidence;
            IsPrecise = isPrecise;
        }

        public string MemoryKey { get; }
        public string SourceEntityId { get; }
        public AIPerceptionChannel Channel { get; }
        public Vector3 LastKnownPosition { get; }
        public Vector3 Direction { get; }
        public double ObservedAt { get; }
        public double ExpiresAt { get; }
        public float UncertaintyRadius { get; }
        public float InitialConfidence { get; }
        public float Confidence { get; }
        public bool IsPrecise { get; }
    }
}
