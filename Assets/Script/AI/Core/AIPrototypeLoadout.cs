using UnityEngine;

namespace CGame
{
    [CreateAssetMenu(fileName = "AIPrototypeLoadout", menuName = "CGame/AI/Prototype Loadout")]
    public sealed class AIPrototypeLoadout : ScriptableObject
    {
        [SerializeField]
        private GameObject riflePrefab;

        [SerializeField]
        private WeaponProfile weaponProfile;

        [SerializeField]
        private PerceptionProfile perceptionProfile;

        [SerializeField]
        private DecisionProfile decisionProfile;

        [SerializeField]
        private CombatProfile combatProfile;

        [SerializeField]
        [Min(0.01f)]
        private float maxHealth = 100f;

        public GameObject RiflePrefab => riflePrefab;
        public WeaponProfile WeaponProfile => weaponProfile;
        public PerceptionProfile PerceptionProfile => perceptionProfile;
        public DecisionProfile DecisionProfile => decisionProfile;
        public CombatProfile CombatProfile => combatProfile;
        public float MaxHealth => maxHealth;
        public bool IsValid => riflePrefab != null
            && riflePrefab.TryGetComponent(out PrototypeRifleView view)
            && view.IsValid
            && weaponProfile != null
            && weaponProfile.IsValid
            && perceptionProfile != null
            && perceptionProfile.IsValid
            && decisionProfile != null
            && decisionProfile.IsValid
            && combatProfile != null
            && combatProfile.IsValid
            && maxHealth > 0f;
    }
}
