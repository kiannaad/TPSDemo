namespace CGame
{
    public sealed class AICombatDebugSnapshot
    {
        public AICombatDebugSnapshot(
            string runtimeId,
            bool isAlive,
            AIPerceptionDebugSnapshot perception,
            AINavigationDebugSnapshot navigation,
            AIDecisionDebugSnapshot decision,
            AICoverCombatDebugSnapshot coverCombat,
            AISquadDebugSnapshot squad)
        {
            RuntimeId = runtimeId ?? string.Empty;
            IsAlive = isAlive;
            Perception = perception;
            Navigation = navigation;
            Decision = decision;
            CoverCombat = coverCombat;
            Squad = squad;
        }

        public string RuntimeId { get; }
        public bool IsAlive { get; }
        public AIPerceptionDebugSnapshot Perception { get; }
        public AINavigationDebugSnapshot Navigation { get; }
        public AIDecisionDebugSnapshot Decision { get; }
        public AICoverCombatDebugSnapshot CoverCombat { get; }
        public AISquadDebugSnapshot Squad { get; }
    }
}
