using UnityEngine;

namespace CGame
{
    public readonly struct CoverEvaluationContext
    {
        public CoverEvaluationContext(
            bool isReachable,
            bool blocksThreat,
            bool hasLineOfFire,
            float distanceToAI,
            float distanceToThreat,
            float exposure,
            float pathRisk,
            CoverStance stance,
            bool isOccupied)
        {
            IsReachable = isReachable;
            BlocksThreat = blocksThreat;
            HasLineOfFire = hasLineOfFire;
            DistanceToAI = Mathf.Max(0f, distanceToAI);
            DistanceToThreat = Mathf.Max(0f, distanceToThreat);
            Exposure = Mathf.Clamp01(exposure);
            PathRisk = Mathf.Clamp01(pathRisk);
            Stance = stance;
            IsOccupied = isOccupied;
        }

        public bool IsReachable { get; }
        public bool BlocksThreat { get; }
        public bool HasLineOfFire { get; }
        public float DistanceToAI { get; }
        public float DistanceToThreat { get; }
        public float Exposure { get; }
        public float PathRisk { get; }
        public CoverStance Stance { get; }
        public bool IsOccupied { get; }
    }
}
