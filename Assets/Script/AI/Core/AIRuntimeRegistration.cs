using UnityEngine;

namespace CGame
{
    public sealed class AIRuntimeRegistration
    {
        private bool isActive = true;

        internal AIRuntimeRegistration(
            CharacterRuntimeId runtimeId,
            AIController controller,
            Transform runtimeTransform,
            HealthComponent health,
            WeaponRuntimeBehaviour weaponRuntime,
            Transform muzzle,
            Transform leftHandSupport,
            Transform rightHandGrip)
        {
            RuntimeId = runtimeId;
            Controller = controller;
            Transform = runtimeTransform;
            Health = health;
            WeaponRuntime = weaponRuntime;
            Muzzle = muzzle;
            LeftHandSupport = leftHandSupport;
            RightHandGrip = rightHandGrip;
        }

        public CharacterRuntimeId RuntimeId { get; }
        public AIController Controller { get; }
        public Transform Transform { get; }
        public HealthComponent Health { get; }
        public WeaponRuntimeBehaviour WeaponRuntime { get; }
        public Transform Muzzle { get; }
        public Transform LeftHandSupport { get; }
        public Transform RightHandGrip { get; }
        public AINavigationRuntimeBehaviour Navigation { get; private set; }
        public AIPerceptionRuntimeBehaviour Perception { get; private set; }
        public AIAlertDecisionRuntimeBehaviour Decision { get; private set; }
        public AICoverCombatRuntimeBehaviour CoverCombat { get; private set; }
        public AISquadMemberRuntimeBehaviour SquadMember { get; private set; }
        public AICombatDebugRuntimeBehaviour DebugRuntime { get; private set; }
        public bool IsActive => isActive;
        public bool IsAlive => Health != null && Health.IsAlive;

        public bool SubmitControlFrame(AIControlFrame frame)
        {
            if (!isActive || Controller == null)
            {
                return false;
            }

            Controller.SubmitControlFrame(frame);
            return true;
        }

        internal void Deactivate()
        {
            DebugRuntime?.Shutdown();
            DebugRuntime = null;
            CoverCombat?.Shutdown();
            CoverCombat = null;
            Decision?.Shutdown();
            Decision = null;
            SquadMember?.Shutdown();
            SquadMember = null;
            Perception?.Shutdown();
            Perception = null;
            Navigation?.Shutdown();
            Navigation = null;
            isActive = false;
        }

        internal void AttachNavigation(AINavigationRuntimeBehaviour navigation)
        {
            Navigation = navigation;
        }

        internal void AttachPerception(AIPerceptionRuntimeBehaviour perception)
        {
            Perception = perception;
        }

        internal void AttachDecision(AIAlertDecisionRuntimeBehaviour decision)
        {
            Decision = decision;
        }

        internal void AttachCoverCombat(AICoverCombatRuntimeBehaviour coverCombat)
        {
            CoverCombat = coverCombat;
        }

        internal void AttachSquadMember(AISquadMemberRuntimeBehaviour squadMember)
        {
            SquadMember = squadMember;
        }

        internal void AttachDebugRuntime(AICombatDebugRuntimeBehaviour debugRuntime)
        {
            DebugRuntime = debugRuntime;
        }
    }
}
