using UnityEngine;

namespace CGame
{
    public readonly struct AIDecisionContext
    {
        public AIDecisionContext(
            AIAlertState state,
            Vector3 position,
            bool hasThreat,
            Vector3 threatPosition,
            float threatConfidence,
            float healthNormalized,
            double timestamp)
        {
            State = state;
            Position = position;
            HasThreat = hasThreat;
            ThreatPosition = threatPosition;
            ThreatConfidence = Mathf.Clamp01(threatConfidence);
            HealthNormalized = Mathf.Clamp01(healthNormalized);
            Timestamp = timestamp;
        }

        public AIAlertState State { get; }
        public Vector3 Position { get; }
        public bool HasThreat { get; }
        public Vector3 ThreatPosition { get; }
        public float ThreatConfidence { get; }
        public float HealthNormalized { get; }
        public double Timestamp { get; }
        public float ThreatDistance => HasThreat
            ? Vector3.Distance(Position, ThreatPosition)
            : 0f;
    }
}
