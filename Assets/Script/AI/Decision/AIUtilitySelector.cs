using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGame
{
    public sealed class AIUtilitySelector
    {
        private readonly DecisionProfile profile;
        private readonly int seed;
        private readonly Dictionary<AIActionKind, double> cooldownUntil =
            new Dictionary<AIActionKind, double>();
        private readonly List<AIUtilityCandidate> candidates = new List<AIUtilityCandidate>();

        public AIUtilitySelector(DecisionProfile profile, int seed)
        {
            this.profile = profile != null && profile.IsValid
                ? profile
                : throw new ArgumentException("A valid decision profile is required.", nameof(profile));
            this.seed = seed;
        }

        public AIUtilitySelection Select(AIDecisionContext context)
        {
            candidates.Clear();
            switch (context.State)
            {
                case AIAlertState.Patrol:
                    Add(context, AIActionKind.Hold, 0.8f, "patrol-hold");
                    Add(context, AIActionKind.SearchPoint, 0.35f, "patrol-observe");
                    break;
                case AIAlertState.Investigate:
                    Add(context, AIActionKind.Approach, 0.75f + context.ThreatConfidence * 0.1f, "approach-last-known");
                    Add(context, AIActionKind.SearchPoint, 0.6f, "inspect-area");
                    Add(context, AIActionKind.Hold, 0.25f, "investigate-pause");
                    break;
                case AIAlertState.Combat:
                    float distance = context.ThreatDistance;
                    float preferred = profile.PreferredCombatDistance;
                    float tolerance = profile.CombatDistanceTolerance;
                    Add(context, AIActionKind.Aim, 0.85f + context.ThreatConfidence * 0.1f, "confirmed-threat");
                    Add(context, AIActionKind.Approach,
                        distance > preferred + tolerance ? 0.95f : 0.2f,
                        "combat-distance-far");
                    Add(context, AIActionKind.Retreat,
                        distance < preferred - tolerance
                            ? 0.95f + (1f - context.HealthNormalized) * 0.1f
                            : 0.15f,
                        "combat-distance-close");
                    Add(context, AIActionKind.Hold, 0.45f, "combat-hold");
                    break;
                case AIAlertState.Search:
                    Add(context, AIActionKind.SearchPoint, 0.9f, "search-last-known-area");
                    Add(context, AIActionKind.Hold, 0.3f, "search-pause");
                    break;
                case AIAlertState.Return:
                    Add(context, AIActionKind.Approach, 0.85f, "return-home");
                    Add(context, AIActionKind.Hold, 0.25f, "return-pause");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (candidates.Count == 0)
            {
                return new AIUtilitySelection(
                    AIActionKind.Hold,
                    "all-candidates-on-cooldown",
                    Array.Empty<AIUtilityCandidate>());
            }

            candidates.Sort(CompareCandidates);
            AIUtilityCandidate selected = candidates[0];
            return new AIUtilitySelection(
                selected.Kind,
                selected.Reason,
                candidates.ToArray());
        }

        public void SetCooldown(AIActionKind kind, double until)
        {
            cooldownUntil[kind] = until;
        }

        public void Clear()
        {
            cooldownUntil.Clear();
            candidates.Clear();
        }

        private void Add(
            AIDecisionContext context,
            AIActionKind kind,
            float score,
            string reason)
        {
            if (cooldownUntil.TryGetValue(kind, out double until) && context.Timestamp < until)
            {
                return;
            }

            float jitter = DeterministicJitter(context.State, kind);
            candidates.Add(new AIUtilityCandidate(kind, Mathf.Max(0f, score + jitter), reason));
        }

        private float DeterministicJitter(AIAlertState state, AIActionKind kind)
        {
            unchecked
            {
                uint value = (uint)seed;
                value = value * 16777619u ^ (uint)state;
                value = value * 16777619u ^ (uint)kind;
                float normalized = (value & 0xFFFFu) / 65535f;
                return (normalized * 2f - 1f) * profile.UtilityJitter;
            }
        }

        private static int CompareCandidates(AIUtilityCandidate left, AIUtilityCandidate right)
        {
            int scoreOrder = right.Score.CompareTo(left.Score);
            return scoreOrder != 0
                ? scoreOrder
                : left.Kind.CompareTo(right.Kind);
        }
    }
}
