using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGame
{
    public sealed class CoverScorer
    {
        private readonly CombatProfile profile;

        public CoverScorer(CombatProfile profile)
        {
            this.profile = profile != null && profile.IsValid
                ? profile
                : throw new ArgumentException("A valid combat profile is required.", nameof(profile));
        }

        public CoverCandidateScore Evaluate(string slotId, CoverEvaluationContext context)
        {
            var reasons = new List<string>();
            if (!context.IsReachable)
            {
                reasons.Add("unreachable");
            }
            if (!context.BlocksThreat)
            {
                reasons.Add("no-occlusion");
            }
            if (!context.HasLineOfFire)
            {
                reasons.Add("no-line-of-fire");
            }
            if (context.IsOccupied)
            {
                reasons.Add("occupied");
            }

            bool viable = reasons.Count == 0;
            if (!viable)
            {
                return new CoverCandidateScore(slotId, float.NegativeInfinity, false, reasons.ToArray());
            }

            float distanceError = Mathf.Abs(context.DistanceToThreat - profile.PreferredDistance);
            float distanceScore = 1f / (1f + distanceError);
            float score = profile.ReachableWeight
                + profile.OcclusionWeight
                + profile.LineOfFireWeight
                + distanceScore * profile.DistanceWeight
                - context.Exposure * profile.ExposurePenalty
                - context.PathRisk * profile.PathRiskPenalty;
            reasons.Add("reachable");
            reasons.Add("occluded");
            reasons.Add("line-of-fire");
            reasons.Add($"distance:{distanceScore:0.###}");
            reasons.Add($"exposure:{context.Exposure:0.###}");
            reasons.Add($"path-risk:{context.PathRisk:0.###}");
            if (context.Stance == CoverStance.Standing)
            {
                score += profile.StandingBonus;
                reasons.Add("standing");
            }
            else
            {
                reasons.Add("crouching");
            }

            return new CoverCandidateScore(slotId, score, true, reasons.ToArray());
        }
    }
}
