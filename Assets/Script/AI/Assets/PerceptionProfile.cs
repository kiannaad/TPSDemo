using UnityEngine;

namespace CGame
{
    [CreateAssetMenu(fileName = "PerceptionProfile", menuName = "CGame/AI/Perception Profile")]
    public sealed class PerceptionProfile : ScriptableObject
    {
        [SerializeField]
        [Min(0.01f)]
        private float viewDistance = 20f;

        [SerializeField]
        [Range(0.01f, 360f)]
        private float horizontalFieldOfView = 100f;

        [SerializeField]
        [Min(0f)]
        private float recognitionDuration = 0.25f;

        [SerializeField]
        [Min(0.01f)]
        private float perceptionInterval = 0.1f;

        [SerializeField]
        [Min(0.01f)]
        private float visualMemoryDuration = 5f;

        [SerializeField]
        [Min(0.01f)]
        private float soundMemoryDuration = 4f;

        [SerializeField]
        [Min(0.01f)]
        private float damageMemoryDuration = 6f;

        [SerializeField]
        [Range(0f, 1f)]
        private float visualConfidence = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        private float soundConfidence = 0.55f;

        [SerializeField]
        [Range(0f, 1f)]
        private float damageConfidence = 0.8f;

        [SerializeField]
        [Min(0f)]
        private float defaultSoundUncertainty = 6f;

        [SerializeField]
        private LayerMask occlusionMask = ~0;

        public float ViewDistance => viewDistance;
        public float HorizontalFieldOfView => horizontalFieldOfView;
        public float RecognitionDuration => recognitionDuration;
        public float PerceptionInterval => perceptionInterval;
        public float VisualMemoryDuration => visualMemoryDuration;
        public float SoundMemoryDuration => soundMemoryDuration;
        public float DamageMemoryDuration => damageMemoryDuration;
        public float VisualConfidence => visualConfidence;
        public float SoundConfidence => soundConfidence;
        public float DamageConfidence => damageConfidence;
        public float DefaultSoundUncertainty => defaultSoundUncertainty;
        public LayerMask OcclusionMask => occlusionMask;

        public bool IsValid => viewDistance > 0f
            && horizontalFieldOfView > 0f
            && horizontalFieldOfView <= 360f
            && recognitionDuration >= 0f
            && perceptionInterval > 0f
            && visualMemoryDuration > 0f
            && soundMemoryDuration > 0f
            && damageMemoryDuration > 0f
            && visualConfidence >= 0f
            && visualConfidence <= 1f
            && soundConfidence >= 0f
            && soundConfidence <= 1f
            && damageConfidence >= 0f
            && damageConfidence <= 1f
            && defaultSoundUncertainty >= 0f;
    }
}
