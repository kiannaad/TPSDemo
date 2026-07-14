using System;
using UnityEngine;

namespace CGame
{
    public readonly struct AIStimulus
    {
        private AIStimulus(
            AIPerceptionChannel channel,
            string sourceEntityId,
            Vector3 position,
            Vector3 direction,
            double timestamp,
            float uncertaintyRadius,
            float confidence,
            bool isPrecise)
        {
            Channel = channel;
            SourceEntityId = sourceEntityId ?? string.Empty;
            Position = position;
            Direction = direction.sqrMagnitude > 0.000001f ? direction.normalized : Vector3.zero;
            Timestamp = timestamp;
            UncertaintyRadius = Mathf.Max(0f, uncertaintyRadius);
            Confidence = Mathf.Clamp01(confidence);
            IsPrecise = isPrecise;
        }

        public AIPerceptionChannel Channel { get; }
        public string SourceEntityId { get; }
        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public double Timestamp { get; }
        public float UncertaintyRadius { get; }
        public float Confidence { get; }
        public bool IsPrecise { get; }

        public bool IsValid => !double.IsNaN(Timestamp)
            && !double.IsInfinity(Timestamp)
            && !float.IsNaN(Confidence)
            && !float.IsInfinity(Confidence)
            && Confidence > 0f;

        public static AIStimulus CreateVisual(
            string sourceEntityId,
            Vector3 position,
            double timestamp,
            float confidence)
        {
            if (string.IsNullOrWhiteSpace(sourceEntityId))
            {
                throw new ArgumentException("Visual stimuli require a source entity ID.", nameof(sourceEntityId));
            }

            return new AIStimulus(
                AIPerceptionChannel.Visual,
                sourceEntityId,
                position,
                Vector3.zero,
                timestamp,
                0f,
                confidence,
                true);
        }

        public static AIStimulus CreateSound(
            string sourceEntityId,
            Vector3 position,
            double timestamp,
            float uncertaintyRadius,
            float confidence)
        {
            return new AIStimulus(
                AIPerceptionChannel.Sound,
                sourceEntityId,
                position,
                Vector3.zero,
                timestamp,
                uncertaintyRadius,
                confidence,
                false);
        }

        public static AIStimulus CreateDamage(
            string sourceEntityId,
            Vector3 position,
            Vector3 direction,
            double timestamp,
            float confidence)
        {
            return new AIStimulus(
                AIPerceptionChannel.Damage,
                sourceEntityId,
                position,
                direction,
                timestamp,
                0f,
                confidence,
                !string.IsNullOrWhiteSpace(sourceEntityId));
        }
    }
}
