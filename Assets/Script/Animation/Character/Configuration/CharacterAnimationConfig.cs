using UnityEngine;

namespace CGame.Animation
{
    [CreateAssetMenu(menuName = "CGame/Animation/Character Animation Config", fileName = "CharacterAnimationConfig")]
    public sealed class CharacterAnimationConfig : ScriptableObject
    {
        [SerializeField] private AnimationClipAsset idle;
        [SerializeField] private AnimationClipAsset walk;
        [SerializeField] private AnimationClipAsset run;
        [SerializeField] private AnimationClipAsset stop;
        [SerializeField] private AnimationClipAsset jumpStart;
        [SerializeField] private AnimationClipAsset inAir;
        [SerializeField] private AnimationClipAsset land;
        [SerializeField] private WeaponAnimationDefinition[] weaponDefinitions;

        public AnimationClipAsset Idle => idle;
        public AnimationClipAsset Walk => walk;
        public AnimationClipAsset Run => run;
        public AnimationClipAsset Stop => stop;
        public AnimationClipAsset JumpStart => jumpStart;
        public AnimationClipAsset InAir => inAir;
        public AnimationClipAsset Land => land;
        public WeaponAnimationDefinition[] WeaponDefinitions => weaponDefinitions;

        public bool IsValid => IsValidAsset(idle)
            && IsValidAsset(walk)
            && IsValidAsset(run)
            && IsValidAsset(stop)
            && IsValidAsset(jumpStart)
            && IsValidAsset(inAir)
            && IsValidAsset(land);

        private static bool IsValidAsset(AnimationClipAsset asset) => asset != null && asset.IsValid;
    }
}
