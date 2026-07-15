using UnityEngine;

namespace CGame
{
    public readonly struct AISquadSuggestion
    {
        public AISquadSuggestion(
            string subjectId,
            Vector3 estimatedPosition,
            double reportedAt,
            float confidence,
            float uncertaintyRadius)
        {
            SubjectId = subjectId;
            EstimatedPosition = estimatedPosition;
            ReportedAt = reportedAt;
            Confidence = Mathf.Clamp01(confidence);
            UncertaintyRadius = Mathf.Max(0.01f, uncertaintyRadius);
        }

        public string SubjectId { get; }
        public Vector3 EstimatedPosition { get; }
        public double ReportedAt { get; }
        public float Confidence { get; }
        public float UncertaintyRadius { get; }
        public bool CanAuthorizeFire => false;

        public bool ShouldAccept(bool isAlive, bool pathReachable, float minimumConfidence)
        {
            return isAlive && pathReachable && Confidence >= minimumConfidence;
        }
    }
}
