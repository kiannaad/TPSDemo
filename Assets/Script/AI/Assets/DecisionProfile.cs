using System;
using UnityEngine;

namespace CGame
{
    [CreateAssetMenu(fileName = "DecisionProfile", menuName = "CGame/AI/Decision Profile")]
    public sealed class DecisionProfile : ScriptableObject
    {
        [SerializeField]
        [Min(0f)]
        private float minimumCommitment = 0.5f;

        [SerializeField]
        [Min(0f)]
        private float actionCooldown = 0.5f;

        [SerializeField]
        [Min(0.01f)]
        private float reevaluationInterval = 0.2f;

        [SerializeField]
        [Min(0.01f)]
        private float searchDuration = 2f;

        [SerializeField]
        [Min(0.01f)]
        private float returnDuration = 1f;

        [SerializeField]
        [Min(0.01f)]
        private float holdDuration = 0.75f;

        [SerializeField]
        [Min(0.01f)]
        private float aimDuration = 0.8f;

        [SerializeField]
        [Min(0.01f)]
        private float approachDuration = 1f;

        [SerializeField]
        [Min(0.01f)]
        private float retreatDuration = 0.8f;

        [SerializeField]
        [Min(0.01f)]
        private float searchPointDuration = 1f;

        [SerializeField]
        [Min(0.01f)]
        private float preferredCombatDistance = 8f;

        [SerializeField]
        [Min(0f)]
        private float combatDistanceTolerance = 2f;

        [SerializeField]
        [Min(0.01f)]
        private float retreatDistance = 3f;

        [SerializeField]
        [Range(0f, 0.25f)]
        private float utilityJitter = 0.02f;

        [SerializeField]
        [Range(0f, 1f)]
        private float combatConfidenceThreshold = 0.65f;

        public float MinimumCommitment => minimumCommitment;
        public float ActionCooldown => actionCooldown;
        public float ReevaluationInterval => reevaluationInterval;
        public float SearchDuration => searchDuration;
        public float ReturnDuration => returnDuration;
        public float PreferredCombatDistance => preferredCombatDistance;
        public float CombatDistanceTolerance => combatDistanceTolerance;
        public float RetreatDistance => retreatDistance;
        public float UtilityJitter => utilityJitter;
        public float CombatConfidenceThreshold => combatConfidenceThreshold;

        public bool IsValid => minimumCommitment >= 0f
            && actionCooldown >= 0f
            && reevaluationInterval > 0f
            && searchDuration > 0f
            && returnDuration > 0f
            && holdDuration > 0f
            && aimDuration > 0f
            && approachDuration > 0f
            && retreatDuration > 0f
            && searchPointDuration > 0f
            && preferredCombatDistance > 0f
            && combatDistanceTolerance >= 0f
            && retreatDistance > 0f
            && utilityJitter >= 0f
            && utilityJitter <= 0.25f
            && combatConfidenceThreshold >= 0f
            && combatConfidenceThreshold <= 1f;

        public float GetMaximumDuration(AIActionKind kind)
        {
            switch (kind)
            {
                case AIActionKind.Hold:
                    return holdDuration;
                case AIActionKind.Aim:
                    return aimDuration;
                case AIActionKind.Approach:
                    return approachDuration;
                case AIActionKind.Retreat:
                    return retreatDuration;
                case AIActionKind.SearchPoint:
                    return searchPointDuration;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }
}
