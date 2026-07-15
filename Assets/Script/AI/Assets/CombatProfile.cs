using UnityEngine;

namespace CGame
{
    [CreateAssetMenu(fileName = "CombatProfile", menuName = "CGame/AI/Combat Profile")]
    public sealed class CombatProfile : ScriptableObject
    {
        [SerializeField, Min(0.01f)]
        private float aimConvergenceDuration = 0.45f;

        [SerializeField, Min(1)]
        private int burstLength = 3;

        [SerializeField, Min(0.01f)]
        private float burstInterval = 0.65f;

        [SerializeField, Min(0.01f)]
        private float preferredDistance = 8f;

        [SerializeField, Min(0f)]
        private float distanceTolerance = 2f;

        [SerializeField, Min(0f)]
        private float baseAimErrorDegrees = 0.25f;

        [SerializeField, Min(0f)]
        private float unconvergedAimErrorDegrees = 5f;

        [SerializeField, Min(0f)]
        private float movementAimErrorDegrees = 2f;

        [SerializeField, Min(0f)]
        private float pressureAimErrorDegrees = 3f;

        [SerializeField, Min(0f)]
        private float reachableWeight = 1f;

        [SerializeField, Min(0f)]
        private float occlusionWeight = 2f;

        [SerializeField, Min(0f)]
        private float lineOfFireWeight = 1.5f;

        [SerializeField, Min(0f)]
        private float distanceWeight = 1f;

        [SerializeField, Min(0f)]
        private float exposurePenalty = 2f;

        [SerializeField, Min(0f)]
        private float pathRiskPenalty = 1.5f;

        [SerializeField, Min(0f)]
        private float standingBonus = 0.15f;

        [SerializeField, Min(0.01f)]
        private float repositionInterval = 3f;

        [SerializeField, Range(0f, 1f)]
        private float lowHealthThreshold = 0.35f;

        public float AimConvergenceDuration => aimConvergenceDuration;
        public int BurstLength => burstLength;
        public float BurstInterval => burstInterval;
        public float PreferredDistance => preferredDistance;
        public float DistanceTolerance => distanceTolerance;
        public float BaseAimErrorDegrees => baseAimErrorDegrees;
        public float UnconvergedAimErrorDegrees => unconvergedAimErrorDegrees;
        public float MovementAimErrorDegrees => movementAimErrorDegrees;
        public float PressureAimErrorDegrees => pressureAimErrorDegrees;
        public float ReachableWeight => reachableWeight;
        public float OcclusionWeight => occlusionWeight;
        public float LineOfFireWeight => lineOfFireWeight;
        public float DistanceWeight => distanceWeight;
        public float ExposurePenalty => exposurePenalty;
        public float PathRiskPenalty => pathRiskPenalty;
        public float StandingBonus => standingBonus;
        public float RepositionInterval => repositionInterval;
        public float LowHealthThreshold => lowHealthThreshold;

        public bool IsValid => aimConvergenceDuration > 0f
            && burstLength > 0
            && burstInterval > 0f
            && preferredDistance > 0f
            && distanceTolerance >= 0f
            && baseAimErrorDegrees >= 0f
            && unconvergedAimErrorDegrees >= 0f
            && movementAimErrorDegrees >= 0f
            && pressureAimErrorDegrees >= 0f
            && repositionInterval > 0f
            && lowHealthThreshold >= 0f
            && lowHealthThreshold <= 1f;
    }
}
