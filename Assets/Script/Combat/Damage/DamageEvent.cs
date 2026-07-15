using System;
using UnityEngine;

namespace CGame
{
    public readonly struct DamageEvent
    {
        public DamageEvent(
            string eventId,
            string sourceEntityId,
            string targetEntityId,
            float amount,
            Vector3 hitPoint,
            Vector3 direction,
            double timestamp)
        {
            EventId = eventId;
            SourceEntityId = sourceEntityId;
            TargetEntityId = targetEntityId;
            Amount = amount;
            HitPoint = hitPoint;
            Direction = direction.sqrMagnitude > 0.000001f ? direction.normalized : Vector3.zero;
            Timestamp = timestamp;
        }

        public string EventId { get; }
        public string SourceEntityId { get; }
        public string TargetEntityId { get; }
        public float Amount { get; }
        public Vector3 HitPoint { get; }
        public Vector3 Direction { get; }
        public double Timestamp { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(EventId)
            && !float.IsNaN(Amount)
            && !float.IsInfinity(Amount)
            && Amount > 0f
            && !double.IsNaN(Timestamp)
            && !double.IsInfinity(Timestamp);
    }
}
