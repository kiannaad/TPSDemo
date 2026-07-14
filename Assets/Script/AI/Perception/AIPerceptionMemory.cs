using System;
using System.Collections.Generic;

namespace CGame
{
    public sealed class AIPerceptionMemory
    {
        private readonly PerceptionProfile profile;
        private readonly Dictionary<string, AIPerceptionMemoryRecord> records =
            new Dictionary<string, AIPerceptionMemoryRecord>();
        private readonly List<string> expiredKeys = new List<string>();
        private readonly List<AIPerceptionMemoryRecord> updatedRecords =
            new List<AIPerceptionMemoryRecord>();
        private int anonymousSequence;

        public AIPerceptionMemory(PerceptionProfile profile)
        {
            this.profile = profile != null && profile.IsValid
                ? profile
                : throw new ArgumentException("A valid perception profile is required.", nameof(profile));
        }

        public int Count => records.Count;

        public void Observe(AIStimulus stimulus)
        {
            if (!stimulus.IsValid)
            {
                return;
            }

            string key = !string.IsNullOrWhiteSpace(stimulus.SourceEntityId)
                ? stimulus.SourceEntityId
                : $"{stimulus.Channel}:anonymous:{++anonymousSequence}";
            double lifetime = GetLifetime(stimulus.Channel);
            records[key] = new AIPerceptionMemoryRecord(
                key,
                stimulus.SourceEntityId,
                stimulus.Channel,
                stimulus.Position,
                stimulus.Direction,
                stimulus.Timestamp,
                stimulus.Timestamp + lifetime,
                stimulus.UncertaintyRadius,
                stimulus.Confidence,
                stimulus.Confidence,
                stimulus.IsPrecise);
        }

        public void Advance(double timestamp)
        {
            if (double.IsNaN(timestamp) || double.IsInfinity(timestamp))
            {
                throw new ArgumentOutOfRangeException(nameof(timestamp));
            }

            expiredKeys.Clear();
            updatedRecords.Clear();
            foreach (KeyValuePair<string, AIPerceptionMemoryRecord> pair in records)
            {
                AIPerceptionMemoryRecord record = pair.Value;
                double lifetime = record.ExpiresAt - record.ObservedAt;
                if (timestamp >= record.ExpiresAt || lifetime <= 0d)
                {
                    expiredKeys.Add(pair.Key);
                    continue;
                }

                float remaining = (float)Math.Max(0d, Math.Min(1d, (record.ExpiresAt - timestamp) / lifetime));
                updatedRecords.Add(new AIPerceptionMemoryRecord(
                    record.MemoryKey,
                    record.SourceEntityId,
                    record.Channel,
                    record.LastKnownPosition,
                    record.Direction,
                    record.ObservedAt,
                    record.ExpiresAt,
                    record.UncertaintyRadius,
                    record.InitialConfidence,
                    remaining * record.InitialConfidence,
                    record.IsPrecise));
            }

            for (int i = 0; i < updatedRecords.Count; i++)
            {
                AIPerceptionMemoryRecord record = updatedRecords[i];
                records[record.MemoryKey] = record;
            }

            for (int i = 0; i < expiredKeys.Count; i++)
            {
                records.Remove(expiredKeys[i]);
            }
        }

        public bool TryGet(string memoryKey, out AIPerceptionMemoryRecord record)
        {
            return records.TryGetValue(memoryKey, out record);
        }

        public AIPerceptionMemoryRecord[] CopyRecords()
        {
            var result = new AIPerceptionMemoryRecord[records.Count];
            records.Values.CopyTo(result, 0);
            return result;
        }

        public void Clear()
        {
            records.Clear();
            expiredKeys.Clear();
            updatedRecords.Clear();
            anonymousSequence = 0;
        }

        private double GetLifetime(AIPerceptionChannel channel)
        {
            switch (channel)
            {
                case AIPerceptionChannel.Visual:
                    return profile.VisualMemoryDuration;
                case AIPerceptionChannel.Sound:
                    return profile.SoundMemoryDuration;
                case AIPerceptionChannel.Damage:
                    return profile.DamageMemoryDuration;
                default:
                    throw new ArgumentOutOfRangeException(nameof(channel));
            }
        }

    }
}
