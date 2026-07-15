using System;
using UnityEngine;

namespace CGame
{
    public readonly struct AISquadReport
    {
        public AISquadReport(
            string reporterId,
            string subjectId,
            Vector3 estimatedPosition,
            double observedAt,
            double deliverAt,
            double expiresAt,
            float confidence,
            float uncertaintyRadius)
        {
            if (string.IsNullOrWhiteSpace(reporterId))
            {
                throw new ArgumentException("A reporter ID is required.", nameof(reporterId));
            }

            if (string.IsNullOrWhiteSpace(subjectId))
            {
                throw new ArgumentException("A subject ID is required.", nameof(subjectId));
            }

            if (deliverAt < observedAt || expiresAt <= deliverAt)
            {
                throw new ArgumentOutOfRangeException(nameof(deliverAt));
            }

            ReporterId = reporterId;
            SubjectId = subjectId;
            EstimatedPosition = estimatedPosition;
            ObservedAt = observedAt;
            DeliverAt = deliverAt;
            ExpiresAt = expiresAt;
            Confidence = Mathf.Clamp01(confidence);
            UncertaintyRadius = Mathf.Max(0.01f, uncertaintyRadius);
        }

        public string ReporterId { get; }
        public string SubjectId { get; }
        public Vector3 EstimatedPosition { get; }
        public double ObservedAt { get; }
        public double DeliverAt { get; }
        public double ExpiresAt { get; }
        public float Confidence { get; }
        public float UncertaintyRadius { get; }
        public bool CanAuthorizeFire => false;

        public bool IsAvailable(double timestamp)
        {
            return timestamp >= DeliverAt && timestamp < ExpiresAt;
        }
    }
}
